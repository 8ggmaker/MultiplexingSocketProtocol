using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Sources;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class PooledValueTaskSourceT : IValueTaskSource
   {
      public void GetResult(short token)
      {
         throw new NotImplementedException();
      }

      public ValueTaskSourceStatus GetStatus(short token)
      {
         throw new NotImplementedException();
      }

      public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
      {
         throw new NotImplementedException();
      }
   }
}
