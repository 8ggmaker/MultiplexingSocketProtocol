using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol.Internal
{
   internal struct WrappedMessage<T>
   {
      public I4ByteMessageId Id { get; private set; }

      public T Payload { get; private set; }

      public WrappedMessage (I4ByteMessageId id, T payload)
      {
         this.Id = id;
         this.Payload = payload;
      }
   }
}
