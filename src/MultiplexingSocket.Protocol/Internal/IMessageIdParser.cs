using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol.Internal
{
   internal interface IMessageIdParser: IMessageReader<I4ByteMessageId>,IMessageWriter<I4ByteMessageId>
   {
     
   }
}
