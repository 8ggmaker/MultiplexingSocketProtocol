using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol.Internal
{
   public class Int32MessageIdParser : IMessageIdParser
   {
      public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out MessageId messageId)
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

      public void WriteMessage(MessageId messageId, IBufferWriter<byte> output)
      {
         if(messageId is Int32MessageId int32Id)
         {
            Span<byte> destination = output.GetSpan(4);
            BinaryPrimitives.WriteInt32BigEndian(destination, int32Id.Id);
            output.Advance(4);
            return;
         }

         throw new InvalidOperationException("can not write non int32 ids");
      }
   }
}
