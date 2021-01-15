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
      private readonly WrappedMessageReader<TInbound> wrappedReader;
      private readonly WrappedMessageWriter<TOutbound> wrappedWriter;
      private readonly IObjectPool<PooledValueTaskSource<MessageId>> sourcePool;

      public MultiplexingSocketProtocol(ConnectionContext connection, IMessageReader<TInbound> messageReader, IMessageWriter<TOutbound> messageWriter, IMessageIdGenerator messageIdGenerator, IMessageIdParser messageIdParser)
      {
         this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
         this.reader = new ProtocolReader(this.connection.Transport.Input);
         this.writer = new ProtocolWriter(this.connection.Transport.Output);
         this.wrappedReader = new WrappedMessageReader<TInbound>(messageIdParser, messageReader);
         this.wrappedWriter = new WrappedMessageWriter<TOutbound>(messageIdParser, messageWriter);
         this.sourcePool = new ObjectPool<PooledValueTaskSource<MessageId>>(() => { return new PooledValueTaskSource<MessageId>(); }, 100);
      }

      public ValueTask<MessageId> Write(TOutbound message,MessageId id)
      {
         var source = this.sourcePool.Rent();
         this.Schedule(this.WriteInternal, new WrappedMessage<TOutbound>(id, message), source);
         return source.Task;
      }
      
      public async ValueTask<Tuple<MessageId,TInbound>> Read()
      {
         var res = await this.reader.ReadAsync<WrappedMessage<TInbound>>(this.wrappedReader);
         if(res.IsCompleted)
         {
            // todo
         }
         else if(res.IsCanceled)
         {
            // todo
         }
         else
         {
            reader.Advance();
         }
         return new Tuple<MessageId, TInbound>(res.Message.Id, res.Message.Payload);
      }

     
      private async ValueTask WriteInternal(WrappedMessage<TOutbound> message,PooledValueTaskSource<MessageId> source)
      {
         try
         {
            await this.writer.WriteAsync<WrappedMessage<TOutbound>>(this.wrappedWriter, message);
            source.SetResult(message.Id);
         }
         catch(Exception ex)
         {
            source.SetException(ex);
         }
      }  
   }
}
