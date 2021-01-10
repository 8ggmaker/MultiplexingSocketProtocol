using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   /// <summary>
   /// not thread-safe, only one writer is allowed at the same time
   /// </summary>
   internal class ProtocolWriter : IAsyncDisposable
   {
      private readonly PipeWriter writer;
      private bool disposed;

      public ProtocolWriter(Stream stream) :
          this(PipeWriter.Create(stream))
      {

      }

      public ProtocolWriter(PipeWriter writer)
      {
         this.writer = writer;
      }

      public ProtocolWriter(PipeWriter writer, SemaphoreSlim semaphore)
      {
         this.writer = writer;
      }

      public async ValueTask WriteAsync<T>(IMessageWriter<T> writer, T protocolMessage, CancellationToken cancellationToken = default)
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

      public async ValueTask WriteManyAsync<T>(IMessageWriter<T> writer, IEnumerable<T> protocolMessages, CancellationToken cancellationToken = default)
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

      public ValueTask DisposeAsync()
      {
         disposed = true;

         return default;
      }

   }
}
