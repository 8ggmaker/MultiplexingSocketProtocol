using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Internal
{
   internal interface IMessageIdGenerator
   {
      ValueTask<MessageId> Next();
   }
}
