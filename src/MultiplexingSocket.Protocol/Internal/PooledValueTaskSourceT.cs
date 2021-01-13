using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Sources;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class PooledValueTaskSource<T> : IValueTaskSource<T>
   {
      private ManualResetValueTaskSourceCore<T> innerSource;
      private IObjectPool<PooledValueTaskSource<T>> pool;

      public short Version => this.innerSource.Version;
      public ValueTaskSourceStatus GetStatus(short token)
      {
         return this.innerSource.GetStatus(token);
      }

      public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
      {
         this.innerSource.OnCompleted(continuation, state, token, flags);
      }

      public T GetResult(short token)
      {
         return this.innerSource.GetResult(token);
      }

      public void Complete()
      {

      }
   }
}
