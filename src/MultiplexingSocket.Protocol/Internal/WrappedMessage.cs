using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol.Internal
{
   internal struct WrappedMessage<T>
   {
      public MessageId Id { get; private set; }

      public T Payload { get; private set; }

      public WrappedMessage (MessageId id, T payload)
      {
         this.Id = id;
         this.Payload = payload;
      }
   }
}
