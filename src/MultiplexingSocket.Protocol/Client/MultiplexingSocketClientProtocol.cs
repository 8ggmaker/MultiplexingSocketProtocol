using MultiplexingSocket.Protocol.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Client
{
   internal class MultiplexingSocketClientProtocol<TInbound, TOutbound> : IMultiplexingSocketClientProtocol<TInbound, TOutbound>
   {
      private ConcurrentDictionary<MessageId, PendingResponse<TInbound>> pendings;
      private IObjectPool<PooledValueTaskSource<TInbound>> sourcePool;
      private IMultiplexingSocketProtocol<TInbound, TOutbound> innerProtocol;
      private IMessageIdGenerator idGenerator;
      public MultiplexingSocketClientProtocol(IMultiplexingSocketProtocol<TInbound,TOutbound> innerProtocol,IMessageIdGenerator idGenerator)
      {
         this.innerProtocol = innerProtocol ?? throw new ArgumentNullException(nameof(innerProtocol));
         this.idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
         this.pendings = new ConcurrentDictionary<MessageId, PendingResponse<TInbound>>();
         this.sourcePool = new ObjectPool<PooledValueTaskSource<TInbound>>(() => {return new PooledValueTaskSource<TInbound>(); }, 100);
         this.ScheduleRead();
      }
      public async ValueTask<TInbound> SendAsync(TOutbound data)
      {
         MessageId id = await idGenerator.Next();
         var source = this.sourcePool.Rent();
         try
         {
            await this.innerProtocol.Write(data, id);
            this.pendings.TryAdd(id, new PendingResponse<TInbound>
            {
               Source = source
            });
         }
         catch (Exception ex)
         {
            source.SetException(ex);
            if (this.pendings.ContainsKey(id))
            {
               this.pendings.TryRemove(id, out PendingResponse<TInbound> removed);
            }
         }
         return await source.Task;

      }

      private void ScheduleRead()
      {
         Task.Run(this.ReadInternal);
      }

      private async Task ReadInternal()
      {
         while(true)
         {
            var next = await this.innerProtocol.Read();
            MessageId id = next.Item1;
            var data = next.Item2;
            if(this.pendings.ContainsKey(id))
            {
               this.pendings[id].Source.SetResult(data);
               this.pendings.TryRemove(id, out PendingResponse<TInbound> removed);
            }
            else
            {
               throw new Exception("wrong data");
            }
         }
      }

      private struct PendingResponse<TInBound>
      {
         public PooledValueTaskSource<TInbound> Source { get; set; }
      }
   }
}
