using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class PooledValueTaskSource<T> : IValueTaskSource<T>
   {
      private ManualResetValueTaskSourceCore<T> innerSource;
      private IObjectPool<PooledValueTaskSource<T>> pool;

      public PooledValueTaskSource()
      {
         this.innerSource = new ManualResetValueTaskSourceCore<T>
         {
            RunContinuationsAsynchronously = true
         };
      }

      public ValueTask<T> Task => new ValueTask<T>(this, this.innerSource.Version);

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

      public void SetResult(T result)
      {
         this.innerSource.SetResult(result);
      }

      public void SetException(Exception ex)
      {
         this.innerSource.SetException(ex);
      }

      public void SetPool(IObjectPool<PooledValueTaskSource<T>> pool)
      {
         this.pool = pool;
      }
      public void Release()
      {
         this.pool?.Return(this);
      }
   }
}
