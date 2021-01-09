using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol
{
   public interface IClientMultiplexingSocketProtocol<TInbound,TOutbound>
   {
      Task<TInbound> SendAsync(TInbound Message);
   }
}
