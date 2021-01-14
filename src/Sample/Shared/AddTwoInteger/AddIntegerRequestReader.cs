using MultiplexingSocket.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Shared.AddTwoInteger
{
   public class AddIntegerRequestReader : IMessageReader<AddIntegerRequest>
   {
      public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out AddIntegerRequest message)
      {
         SequenceReader<byte> seqReader = new SequenceReader<byte>(input);
         if (seqReader.TryReadBigEndian(out int a))
         {
            if (seqReader.TryReadBigEndian(out int b))
            {
               message = new AddIntegerRequest
               {
                  A = a,
                  B = b,
               };
               consumed = seqReader.Position;
               examined = seqReader.Position;
               return true;
            }
         }
         message = default;
         return false;
      }
   }
}
