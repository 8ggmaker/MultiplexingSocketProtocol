using System;
using System.Buffers;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class WrappedMessageReader<T> : IMessageReader<WrappedMessage<T>>
   {
      private readonly IMessageIdParser idParser;
      private readonly IMessageReader<T> reader;

      public WrappedMessageReader(IMessageIdParser idParser,IMessageReader<T> reader)
      {
         this.idParser = idParser ?? throw new ArgumentNullException(nameof(idParser));
         this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
      }

      public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out WrappedMessage<T> message)
      {
         I4ByteMessageId id;
         T payload;
         if(this.idParser.TryParseMessage(input,ref consumed,ref examined,out id))
         {
            ReadOnlySequence<byte> nextInput = input.Slice(consumed);
            if(this.reader.TryParseMessage(nextInput, ref consumed, ref examined,out payload))
            {
               message = new WrappedMessage<T>(id, payload);
               return true;
            }
         }
         message = default;
         return false;
      }
   }
}
