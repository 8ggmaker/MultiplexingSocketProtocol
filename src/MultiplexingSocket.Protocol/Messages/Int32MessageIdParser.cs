using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol.Messages
{
   public class Int32MessageIdParser : IMessageIdParser
   {
      public bool TryParse(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out I4ByteMessageId messageId)
      {
         var reader = new SequenceReader<byte>(input);
         if (reader.TryReadBigEndian(out int id))
         {
            messageId = new Int32MessageId(id);
            consumed = reader.Position;
            examined = consumed;
            return true;
         }

         messageId = default;
         return false;
      }

      public void WriteToSpan(I4ByteMessageId messageId, Span<byte> destination)
      {
         if(messageId is Int32MessageId int32Id)
         {
            BinaryPrimitives.WriteInt32BigEndian(destination, int32Id.Id);
         }

         throw new InvalidOperationException("can not write non int32 ids");
      }
   }
}
