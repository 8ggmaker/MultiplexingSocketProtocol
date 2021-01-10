using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Internal
{
   internal partial class MultiplexingSocketProtocol<TInbound, TOutbound>: IThreadPoolWorkItem
   {
      public void Schedule(Func<I4ByteMessageId,TOutbound,ValueTask> action, object? state)
      {
         workItems.Enqueue(action);

         // Set working if it wasn't (via atomic Interlocked).
         if (Interlocked.CompareExchange(ref doingWork, 1, 0) == 0)
         {
            // Wasn't working, schedule.
            // have to schedule to threadpool to avoid sync path block in Func<object?,ValueTask>
            System.Threading.ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
         }
      }

      void IThreadPoolWorkItem.Execute()
      {
         _ = ExecuteInternal();
      }

      protected virtual async Task ExecuteInternal()
      {
         while (true)
         {
            while (workItems.TryDequeue(out Func<I4ByteMessageId,TOutbound,ValueTask> item))
            {
               await item();
            }

            // All work done.

            // Set _doingWork (0 == false) prior to checking IsEmpty to catch any missed work in interim.
            // This doesn't need to be volatile due to the following barrier (i.e. it is volatile).
            doingWork = 0;

            // Ensure _doingWork is written before IsEmpty is read.
            // As they are two different memory locations, we insert a barrier to guarantee ordering.
            Thread.MemoryBarrier();

            // Check if there is work to do
            if (workItems.IsEmpty)
            {
               // Nothing to do, exit.
               break;
            }

            // Is work, can we set it as active again (via atomic Interlocked), prior to scheduling?
            if (Interlocked.Exchange(ref doingWork, 1) == 1)
            {
               // Execute has been rescheduled already, exit.
               break;
            }

            // Is work, wasn't already scheduled so continue loop.
         }
      }
   }
}
