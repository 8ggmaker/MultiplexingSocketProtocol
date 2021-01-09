using System;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol
{
   public interface IServerMultiplexingSocketProtocol<TInbound, TOutbound>
   {
      void Write(TOutbound message);

      TInbound Read();
   }
}
