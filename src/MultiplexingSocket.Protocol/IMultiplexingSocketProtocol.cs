using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   public interface IMultiplexingSocketProtocol<TInbound,TOutbound>
   {
      ValueTask<I4ByteMessageId> Write(TOutbound message);

      ValueTask<Tuple<I4ByteMessageId,TInbound>> Read();
   }
}
