using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   public interface IMultiplexingSocketProtocol<TInbound,TOutbound>
   {
      I4ByteMessageId Write(TOutbound message);

      TInbound Read();
   }
}
