using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol.Messages
{
   public interface IMessageIdParser
   {
      void WriteToSpan(I4ByteMessageId messageId, Span<byte> destination);
      bool TryParse(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out I4ByteMessageId messageId);
   }
}
