using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol
{
   public interface IMessageReader<T>
   {
      bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out T message);
   }
}
