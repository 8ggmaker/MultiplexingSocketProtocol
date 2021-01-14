using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Client
{
   public interface IMultiplexingSocketClientProtocol<TInbound,TOutbound>
   {
      ValueTask<TInbound> SendAsync(TOutbound data);
   }
}
