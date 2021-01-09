using MultiplexingSocket.Protocol.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   internal class ProtocolReader : IAsyncDisposable
   {
      private readonly PipeReader reader;
      private SequencePosition examined;
      private SequencePosition consumed;
      private ReadOnlySequence<byte> buffer;
      private bool isCanceled;
      private bool isCompleted;
      private bool hasMessage;
      private bool disposed;

      public ProtocolReader(Stream stream) :
          this(PipeReader.Create(stream))
      {

      }

      public ProtocolReader(PipeReader reader)
      {
         this.reader = reader;
      }

      public ValueTask<ProtocolReadResult<T>> ReadAsync<T>(IMessageReader<T> reader, CancellationToken cancellationToken = default)
      {
         return ReadAsync(reader, maximumMessageSize: null, cancellationToken);
      }

      public ValueTask<ProtocolReadResult<T>> ReadAsync<T>(IMessageReader<T> reader, int maximumMessageSize, CancellationToken cancellationToken = default)
      {
         return ReadAsync(reader, (int?)maximumMessageSize, cancellationToken);
      }

      public ValueTask<ProtocolReadResult<T>> ReadAsync<T>(IMessageReader<T> reader, int? maximumMessageSize, CancellationToken cancellationToken = default)
      {
         if (disposed)
         {
            throw new ObjectDisposedException(GetType().Name);
         }

         if (hasMessage)
         {
            throw new InvalidOperationException($"{nameof(Advance)} must be called before calling {nameof(ReadAsync)}");
         }

         // If this is the very first read, then make it go async since we have no data
         if (consumed.GetObject() == null)
         {
            return DoAsyncRead(maximumMessageSize, reader, cancellationToken);
         }

         // We have a buffer, test to see if there's any message left in the buffer
         if (TryParseMessage(maximumMessageSize, reader, buffer, out var protocolMessage))
         {
            hasMessage = true;
            return new ValueTask<ProtocolReadResult<T>>(new ProtocolReadResult<T>(protocolMessage, isCanceled, isCompleted: false));
         }
         else
         {
            // We couldn't parse the message so advance the input so we can read
            this.reader.AdvanceTo(consumed, examined);
         }

         if (isCompleted)
         {
            consumed = default;
            examined = default;

            // If we're complete then short-circuit
            if (!buffer.IsEmpty)
            {
               throw new InvalidDataException("Connection terminated while reading a message.");
            }

            return new ValueTask<ProtocolReadResult<T>>(new ProtocolReadResult<T>(default, isCanceled, isCompleted));
         }

         return DoAsyncRead(maximumMessageSize, reader, cancellationToken);
      }

      private ValueTask<ProtocolReadResult<T>> DoAsyncRead<T>(int? maximumMessageSize, IMessageReader<T> reader, CancellationToken cancellationToken)
      {
         while (true)
         {
            var readTask = this.reader.ReadAsync(cancellationToken);
            ReadResult result;
            if (readTask.IsCompletedSuccessfully)
            {
               result = readTask.Result;
            }
            else
            {
               return ContinueDoAsyncRead(readTask, maximumMessageSize, reader, cancellationToken);
            }

            (var shouldContinue, var hasMessage) = TrySetMessage(result, maximumMessageSize, reader, out var protocolReadResult);
            if (hasMessage)
            {
               return new ValueTask<ProtocolReadResult<T>>(protocolReadResult);
            }
            else if (!shouldContinue)
            {
               break;
            }
         }

         return new ValueTask<ProtocolReadResult<T>>(new ProtocolReadResult<T>(default, isCanceled, isCompleted));
      }

      private async ValueTask<ProtocolReadResult<T>> ContinueDoAsyncRead<T>(ValueTask<ReadResult> readTask, int? maximumMessageSize, IMessageReader<T> reader, CancellationToken cancellationToken)
      {
         while (true)
         {
            var result = await readTask;

            (var shouldContinue, var hasMessage) = TrySetMessage(result, maximumMessageSize, reader, out var protocolReadResult);
            if (hasMessage)
            {
               return protocolReadResult;
            }
            else if (!shouldContinue)
            {
               break;
            }

            readTask = this.reader.ReadAsync(cancellationToken);
         }

         return new ProtocolReadResult<T>(default, isCanceled, isCompleted);
      }

      private (bool ShouldContinue, bool HasMessage) TrySetMessage<T>(ReadResult result, int? maximumMessageSize, IMessageReader<T> reader, out ProtocolReadResult<T> readResult)
      {
         buffer = result.Buffer;
         isCanceled = result.IsCanceled;
         isCompleted = result.IsCompleted;
         consumed = buffer.Start;
         examined = buffer.End;

         if (isCanceled)
         {
            readResult = default;
            return (false, false);
         }

         if (TryParseMessage<T>(maximumMessageSize, reader, buffer, out var protocolMessage))
         {
            hasMessage = true;
            readResult = new ProtocolReadResult<T>(protocolMessage, isCanceled, isCompleted: false);
            return (false, true);
         }
         else
         {
            this.reader.AdvanceTo(consumed, examined);
         }

         if (isCompleted)
         {
            consumed = default;
            examined = default;

            if (!buffer.IsEmpty)
            {
               throw new InvalidDataException("Connection terminated while reading a message.");
            }

            readResult = default;
            return (false, false);
         }

         readResult = default;
         return (true, false);
      }

      private bool TryParseMessage<T>(int? maximumMessageSize, IMessageReader<T> reader, in ReadOnlySequence<byte> buffer, out T protocolMessage)
      {
         // No message limit, just parse and dispatch
         if (maximumMessageSize == null)
         {
            return reader.TryParseMessage(buffer, ref consumed, ref examined, out protocolMessage);
         }

         // We give the parser a sliding window of the default message size
         var maxMessageSize = maximumMessageSize.Value;

         var segment = buffer;
         var overLength = false;

         if (segment.Length > maxMessageSize)
         {
            segment = segment.Slice(segment.Start, maxMessageSize);
            overLength = true;
         }

         if (reader.TryParseMessage(segment, ref consumed, ref examined, out protocolMessage))
         {
            return true;
         }
         else if (overLength)
         {
            throw new InvalidDataException($"The maximum message size of {maxMessageSize}B was exceeded.");
         }

         return false;
      }

      public void Advance()
      {
         if (disposed)
         {
            throw new ObjectDisposedException(GetType().Name);
         }

         isCanceled = false;

         if (!hasMessage)
         {
            return;
         }

         buffer = buffer.Slice(consumed);

         hasMessage = false;
      }

      public ValueTask DisposeAsync()
      {
         disposed = true;
         return default;
      }
   }
}
