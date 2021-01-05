using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol.Messages
{
   internal class Message
   {
      public uint Id { get; private set; }

      public ReadOnlySequence<byte> Payload { get; private set; }

      public Message (uint id, byte[] payload):this(id,new ReadOnlySequence<byte>(payload))
      {

      }

      public Message(uint id, ReadOnlySequence<byte> payload)
      {
         this.Id = id;
         this.Payload = payload;
      }
   }
}
