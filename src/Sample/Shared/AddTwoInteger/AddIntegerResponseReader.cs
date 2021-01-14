using MultiplexingSocket.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Shared.AddTwoInteger
{
   public class AddIntegerResponseReader : IMessageReader<int>
   {
      public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out int message)
      {
         SequenceReader<byte> seqReader = new SequenceReader<byte>(input);
         if (seqReader.TryReadBigEndian(out int res))
         {
            message = res;
            consumed = examined = seqReader.Position;
            return true;
         }
         message = 0;
         return false;
      }
   }
}
