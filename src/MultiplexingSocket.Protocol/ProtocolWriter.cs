using MultiplexingSocket.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   internal class ProtocolWriter : IAsyncDisposable, IThreadPoolWorkItem
   {
      private readonly PipeWriter writer;
      private readonly SemaphoreSlim semaphore;
      private bool disposed;

      public ProtocolWriter(Stream stream) :
          this(PipeWriter.Create(stream))
      {

      }

      public ProtocolWriter(PipeWriter writer)
          : this(writer, new SemaphoreSlim(1))
      {
      }

      public ProtocolWriter(PipeWriter writer, SemaphoreSlim semaphore)
      {
         this.writer = writer;
         this.semaphore = semaphore;
      }

      public async ValueTask WriteAsync<T>(IMessageWriter<T> writer, T protocolMessage, CancellationToken cancellationToken = default)
      {
         await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            if (disposed)
            {
               return;
            }

            writer.WriteMessage(protocolMessage, this.writer);

            var result = await this.writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            if (result.IsCanceled)
            {
               throw new OperationCanceledException();
            }

            if (result.IsCompleted)
            {
               disposed = true;
            }
         }
         finally
         {
            semaphore.Release();
         }
      }

      public async ValueTask WriteManyAsync<T>(IMessageWriter<T> writer, IEnumerable<T> protocolMessages, CancellationToken cancellationToken = default)
      {
         await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            if (disposed)
            {
               return;
            }

            foreach (var protocolMessage in protocolMessages)
            {
               writer.WriteMessage(protocolMessage, this.writer);
            }

            var result = await this.writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            if (result.IsCanceled)
            {
               throw new OperationCanceledException();
            }

            if (result.IsCompleted)
            {
               disposed = true;
            }
         }
         finally
         {
            semaphore.Release();
         }
      }

      public async ValueTask DisposeAsync()
      {
         await semaphore.WaitAsync().ConfigureAwait(false);

         try
         {
            if (disposed)
            {
               return;
            }

            disposed = true;
         }
         finally
         {
            semaphore.Release();
         }
      }

      public void Execute()
      {
         throw new NotImplementedException();
      }
   }
}
