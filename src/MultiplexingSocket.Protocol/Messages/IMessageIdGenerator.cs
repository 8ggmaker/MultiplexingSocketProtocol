using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Messages
{
   public interface IMessageIdGenerator
   {
      ValueTask<I4ByteMessageId> Next();
   }
}
