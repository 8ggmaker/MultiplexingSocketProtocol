using Microsoft.AspNetCore.Connections;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Internal
{
   internal partial class MultiplexingSocketProtocol<TInbound, TOutbound> : IMultiplexingSocketProtocol<TInbound, TOutbound>
   {
      private readonly ConcurrentQueue<Work<TOutbound>> workItems = new ConcurrentQueue<Work<TOutbound>>();
      private int doingWork;
      private readonly ConnectionContext connection;
      private readonly ProtocolReader reader;
      private readonly ProtocolWriter writer;
      private readonly IMessageIdGenerator messageIdGenerator;
      private readonly WrappedMessageReader<TInbound> wrappedReader;
      private readonly WrappedMessageWriter<TOutbound> wrappedWriter;
      private readonly IObjectPool<PooledValueTaskSource<I4ByteMessageId>> sourcePool;

      public MultiplexingSocketProtocol(ConnectionContext connection, IMessageReader<TInbound> messageReader, IMessageWriter<TOutbound> messageWriter, IMessageIdGenerator messageIdGenerator, IMessageIdParser messageIdParser)
      {
         this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
         this.messageIdGenerator = messageIdGenerator ?? throw new ArgumentNullException(nameof(messageIdGenerator));
         this.reader = new ProtocolReader(this.connection.Transport.Input);
         this.writer = new ProtocolWriter(this.connection.Transport.Output);
         this.wrappedReader = new WrappedMessageReader<TInbound>(messageIdParser, messageReader);
         this.wrappedWriter = new WrappedMessageWriter<TOutbound>(messageIdParser, messageWriter);
      }

      public async ValueTask<I4ByteMessageId> Write(TOutbound message)
      {
         I4ByteMessageId id;
         var idTask = this.messageIdGenerator.Next();
         if (idTask.IsCompleted)
         {
            id = idTask.Result;
         }
         else
         {
            id = await idTask;
         }
         var source = this.sourcePool.Rent();
         ValueTask<I4ByteMessageId> task = new ValueTask<I4ByteMessageId>(source, source.Version);
         this.Schedule(this.WriteInternal, new WrappedMessage<TOutbound>(id, message), source);
         return await task;
      }
      
      public async ValueTask<Tuple<I4ByteMessageId,TInbound>> Read()
      {
         var res = await this.reader.ReadAsync<WrappedMessage<TInbound>>(this.wrappedReader);
         if(res.IsCompleted)
         {
            // todo
         }
         if(res.IsCanceled)
         {
            // todo
         }
         return new Tuple<I4ByteMessageId, TInbound>(res.Message.Id, res.Message.Payload);
      }

     
      private async ValueTask WriteInternal(WrappedMessage<TOutbound> message,PooledValueTaskSource<I4ByteMessageId> source)
      {
         await this.writer.WriteAsync<WrappedMessage<TOutbound>>(this.wrappedWriter, message);
         source.Complete();
      }  
   }
}
