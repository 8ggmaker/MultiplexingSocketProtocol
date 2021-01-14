using MultiplexingSocket.Protocol;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Shared.AddTwoInteger
{
   public class AddIntegerResponseWritter : IMessageWriter<int>
   {
      public void WriteMessage(int message, IBufferWriter<byte> output)
      {
         int size = sizeof(int);
         var memory = output.GetSpan(size);
         BinaryPrimitives.WriteInt32BigEndian(memory, message);
      }
   }
}
