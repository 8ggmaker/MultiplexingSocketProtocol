using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Messages
{
   public class Int32MessageIdGenerator : IMessageIdGenerator
   {
      private int next = -1;
      public ValueTask<I4ByteMessageId> Next()
      {
         int res = Interlocked.Increment(ref this.next);
         return new ValueTask<I4ByteMessageId>(new Int32MessageId(res));
      }
   }
}
