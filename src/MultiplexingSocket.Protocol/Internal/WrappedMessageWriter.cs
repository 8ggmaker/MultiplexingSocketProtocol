using System;
using System.Buffers;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class WrappedMessageWriter<T> : IMessageWriter<WrappedMessage<T>>
   {
      private readonly IMessageIdParser idParser;
      private readonly IMessageWriter<T> writer;

      public WrappedMessageWriter(IMessageIdParser idParser, IMessageWriter<T> writer)
      {
         this.idParser = idParser ?? throw new ArgumentNullException(nameof(idParser));
         this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
      }
      public void WriteMessage(WrappedMessage<T> message, IBufferWriter<byte> output)
      {
         this.idParser.WriteMessage(message.Id, output);
         this.writer.WriteMessage(message.Payload,output);
      }
   }
}
