using MultiplexingSocket.Protocol;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Shared.AddTwoInteger
{
   public class AddIntegerRequestWritter : IMessageWriter<AddIntegerRequest>
   {
      public void WriteMessage(AddIntegerRequest message, IBufferWriter<byte> output)
      {
         int size = sizeof(int);
         var memoryA = output.GetSpan(size);
         BinaryPrimitives.WriteInt32BigEndian(memoryA, message.A);
         output.Advance(size);
         var memoryB = output.GetSpan(size);
         BinaryPrimitives.WriteInt32BigEndian(memoryB, message.B);
         output.Advance(size);
      }
   }
}
