using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   public interface IMultiplexingSocketProtocol<TInbound,TOutbound>
   {
      ValueTask<MessageId> Write(TOutbound message, MessageId id);

      ValueTask<Tuple<MessageId,TInbound>> Read();
   }
}
