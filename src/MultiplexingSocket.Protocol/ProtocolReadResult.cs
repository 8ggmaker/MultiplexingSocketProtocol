using MultiplexingSocket.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultiplexingSocket.Protocol
{
   internal readonly struct ProtocolReadResult<T>
   {
      public ProtocolReadResult(T message, bool isCanceled, bool isCompleted)
      {
         Message = message;
         IsCanceled = isCanceled;
         IsCompleted = isCompleted;
      }

      public T Message { get; }
      public bool IsCanceled { get; }
      public bool IsCompleted { get; }
   }
}
