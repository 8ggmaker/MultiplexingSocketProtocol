using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   public interface IMultiplexingSocketProtocol<TInbound,TOutbound>
   {
      ValueTask<I4ByteMessageId> Write(TOutbound message);

      ValueTask<TInbound> Read();
   }
}
