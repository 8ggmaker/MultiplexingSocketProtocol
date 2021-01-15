using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Transport
{
   internal sealed class SocketConnection : ConnectionContext
   {
      private static readonly int MinAllocBufferSize = 4096;
      private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
      private static readonly bool IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

      private readonly Socket socket;
      private readonly ISocketsTrace trace;
      private readonly SocketReceiver receiver;
      private readonly SocketSender sender;
      private readonly CancellationTokenSource connectionClosedTokenSource = new CancellationTokenSource();

      private readonly object shutdownLock = new object();
      private volatile bool socketDisposed;
      private volatile Exception shutdownReason;
      private Task processingTask;
      private readonly TaskCompletionSource<object> waitForConnectionClosedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
      private bool connectionClosed;

      internal SocketConnection(Socket socket,
                                MemoryPool<byte> memoryPool,
                                PipeScheduler scheduler,
                                ISocketsTrace trace,
                                long? maxReadBufferSize = null,
                                long? maxWriteBufferSize = null)
      {
         Debug.Assert(socket != null);
         Debug.Assert(memoryPool != null);
         Debug.Assert(trace != null);

         this.socket = socket;
         this.MemoryPool = memoryPool;
         this.trace = trace;

         this.LocalEndPoint = this.socket.LocalEndPoint;
         this.RemoteEndPoint = this.socket.RemoteEndPoint;

         this.ConnectionClosed = connectionClosedTokenSource.Token;

         // On *nix platforms, Sockets already dispatches to the ThreadPool.
         // Yes, the IOQueues are still used for the PipeSchedulers. This is intentional.
         // https://github.com/aspnet/KestrelHttpServer/issues/2573
         var awaiterScheduler = IsWindows ? scheduler : PipeScheduler.Inline;

         this.receiver = new SocketReceiver(this.socket, awaiterScheduler);
         this.sender = new SocketSender(this.socket, awaiterScheduler);

         maxReadBufferSize ??= 0;
         maxWriteBufferSize ??= 0;

         var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, scheduler, maxReadBufferSize.Value, maxReadBufferSize.Value / 2, useSynchronizationContext: false);
         var outputOptions = new PipeOptions(MemoryPool, scheduler, PipeScheduler.ThreadPool, maxWriteBufferSize.Value, maxWriteBufferSize.Value / 2, useSynchronizationContext: false);

         var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

         // Set the transport and connection id
         this.Transport = pair.Transport;
         this.Input = pair.Application.Output;
         this.Output = pair.Application.Input;
         this.ConnectionId = Guid.NewGuid().ToString();
      }

      public PipeWriter Input { get; private set; }

      public PipeReader Output { get; private set; }

      public MemoryPool<byte> MemoryPool { get; }

      public override IDuplexPipe Transport { get; set; }

      public override string ConnectionId { get; set; }

      public override IFeatureCollection Features => throw new NotImplementedException();

      public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

      public void Start()
      {
         processingTask = StartAsync();
      }

      private async Task StartAsync()
      {
         try
         {
            // Spawn send and receive logic
            var receiveTask = DoReceive();
            var sendTask = DoSend();

            // Now wait for both to complete
            await receiveTask;
            await sendTask;

            receiver.Dispose();
            sender.Dispose();
         }
         catch (Exception ex)
         {
            trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(StartAsync)}.");
         }
      }

      public override void Abort(ConnectionAbortedException abortReason)
      {
         // Try to gracefully close the socket to match libuv behavior.
         Shutdown(abortReason);

         // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
         Output.CancelPendingRead();
      }

      // Only called after connection middleware is complete which means the ConnectionClosed token has fired.
      public override async ValueTask DisposeAsync()
      {
         Transport.Input.Complete();
         Transport.Output.Complete();

         if (processingTask != null)
         {
            await processingTask;
         }

         connectionClosedTokenSource.Dispose();
      }

      private async Task DoReceive()
      {
         Exception error = null;

         try
         {
            await ProcessReceives();
         }
         catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
         {
            // This could be ignored if _shutdownReason is already set.
            error = new ConnectionResetException(ex.Message, ex);

            // There's still a small chance that both DoReceive() and DoSend() can log the same connection reset.
            // Both logs will have the same ConnectionId. I don't think it's worthwhile to lock just to avoid this.
            if (!socketDisposed)
            {
               trace.ConnectionReset(ConnectionId);
            }
         }
         catch (Exception ex)
             when ((ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode)) ||
                    ex is ObjectDisposedException)
         {
            // This exception should always be ignored because _shutdownReason should be set.
            error = ex;

            if (!socketDisposed)
            {
               // This is unexpected if the socket hasn't been disposed yet.
               trace.ConnectionError(ConnectionId, error);
            }
         }
         catch (Exception ex)
         {
            // This is unexpected.
            error = ex;
            trace.ConnectionError(ConnectionId, error);
         }
         finally
         {
            // If Shutdown() has already bee called, assume that was the reason ProcessReceives() exited.
            Input.Complete(shutdownReason ?? error);

            FireConnectionClosed();

            await waitForConnectionClosedTcs.Task;
         }
      }

      private async Task ProcessReceives()
      {
         // Resolve `input` PipeWriter via the IDuplexPipe interface prior to loop start for performance.
         var input = Input;
         while (true)
         {
            // Wait for data before allocating a buffer.
            await receiver.WaitForDataAsync();

            // Ensure we have some reasonable amount of buffer space
            var buffer = input.GetMemory(MinAllocBufferSize);

            var bytesReceived = await receiver.ReceiveAsync(buffer);

            if (bytesReceived == 0)
            {
               // FIN
               trace.ConnectionReadFin(ConnectionId);
               break;
            }

            input.Advance(bytesReceived);

            var flushTask = input.FlushAsync();

            var paused = !flushTask.IsCompleted;

            if (paused)
            {
               trace.ConnectionPause(ConnectionId);
            }

            var result = await flushTask;

            if (paused)
            {
               trace.ConnectionResume(ConnectionId);
            }

            if (result.IsCompleted || result.IsCanceled)
            {
               // Pipe consumer is shut down, do we stop writing
               break;
            }
         }
      }

      private async Task DoSend()
      {
         Exception shutdownReason = null;
         Exception unexpectedError = null;

         try
         {
            await ProcessSends();
         }
         catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
         {
            shutdownReason = new ConnectionResetException(ex.Message, ex);
            trace.ConnectionReset(ConnectionId);
         }
         catch (Exception ex)
             when ((ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode)) ||
                    ex is ObjectDisposedException)
         {
            // This should always be ignored since Shutdown() must have already been called by Abort().
            shutdownReason = ex;
         }
         catch (Exception ex)
         {
            shutdownReason = ex;
            unexpectedError = ex;
            trace.ConnectionError(ConnectionId, unexpectedError);
         }
         finally
         {
            Shutdown(shutdownReason);

            // Complete the output after disposing the socket
            Output.Complete(unexpectedError);

            // Cancel any pending flushes so that the input loop is un-paused
            Input.CancelPendingFlush();
         }
      }

      private async Task ProcessSends()
      {
         // Resolve `output` PipeReader via the IDuplexPipe interface prior to loop start for performance.
         var output = Output;
         while (true)
         {
            var result = await output.ReadAsync();

            if (result.IsCanceled)
            {
               break;
            }

            var buffer = result.Buffer;

            var end = buffer.End;
            var isCompleted = result.IsCompleted;
            if (!buffer.IsEmpty)
            {
               await sender.SendAsync(buffer);
            }

            output.AdvanceTo(end);

            if (isCompleted)
            {
               break;
            }
         }
      }

      private void FireConnectionClosed()
      {
         // Guard against scheduling this multiple times
         if (connectionClosed)
         {
            return;
         }

         connectionClosed = true;

         ThreadPool.UnsafeQueueUserWorkItem(state =>
         {
            state.CancelConnectionClosedToken();

            state.waitForConnectionClosedTcs.TrySetResult(null);
         },
         this,
         preferLocal: false);
      }

      private void Shutdown(Exception shutdownReason)
      {
         lock (shutdownLock)
         {
            if (socketDisposed)
            {
               return;
            }

            // Make sure to close the connection only after the _aborted flag is set.
            // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
            // a BadHttpRequestException is thrown instead of a TaskCanceledException.
            socketDisposed = true;

            // shutdownReason should only be null if the output was completed gracefully, so no one should ever
            // ever observe the nondescript ConnectionAbortedException except for connection middleware attempting
            // to half close the connection which is currently unsupported.
            this.shutdownReason = shutdownReason ?? new ConnectionAbortedException("The Socket transport's send loop completed gracefully.");

            trace.ConnectionWriteFin(ConnectionId, this.shutdownReason.Message);

            try
            {
               // Try to gracefully close the socket even for aborts to match libuv behavior.
               socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
               // Ignore any errors from Socket.Shutdown() since we're tearing down the connection anyway.
            }

            socket.Dispose();
         }
      }

      private void CancelConnectionClosedToken()
      {
         try
         {
            connectionClosedTokenSource.Cancel();
         }
         catch (Exception ex)
         {
            trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(CancelConnectionClosedToken)}.");
         }
      }

      private static bool IsConnectionResetError(SocketError errorCode)
      {
         // A connection reset can be reported as SocketError.ConnectionAborted on Windows.
         // ProtocolType can be removed once https://github.com/dotnet/corefx/issues/31927 is fixed.
         return errorCode == SocketError.ConnectionReset ||
                errorCode == SocketError.Shutdown ||
                (errorCode == SocketError.ConnectionAborted && IsWindows) ||
                (errorCode == SocketError.ProtocolType && IsMacOS);
      }

      private static bool IsConnectionAbortError(SocketError errorCode)
      {
         // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
         return errorCode == SocketError.OperationAborted ||
                errorCode == SocketError.Interrupted ||
                (errorCode == SocketError.InvalidArgument && !IsWindows);
      }
   }
}
