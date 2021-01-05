using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol.Messages
{
   internal interface IMessageReader
   {
      bool TryParseMessages(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out IEnumerable<Message> messages);
   }
}
