using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol.Messages
{
   internal interface IMessageWriter
   {
      void WriteMessage(Message message, IBufferWriter<byte> output);
   }
}
