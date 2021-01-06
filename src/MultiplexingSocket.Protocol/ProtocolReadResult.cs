using MultiplexingSocket.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol
{
   internal readonly struct ProtocolReadResult
   {
      public ProtocolReadResult(Message message, bool isCanceled, bool isCompleted)
      {
         Message = message;
         IsCanceled = isCanceled;
         IsCompleted = isCompleted;
      }

      public Message Message { get; }
      public bool IsCanceled { get; }
      public bool IsCompleted { get; }
   }
}
