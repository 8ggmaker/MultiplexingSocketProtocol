using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class Int32MessageIdGenerator : IMessageIdGenerator
   {
      private int next = -1;
      public ValueTask<MessageId> Next()
      {
         int res = Interlocked.Increment(ref this.next);
         return new ValueTask<MessageId>(new Int32MessageId(res));
      }
   }
}
