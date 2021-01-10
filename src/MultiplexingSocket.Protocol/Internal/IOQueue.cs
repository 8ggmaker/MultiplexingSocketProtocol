using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplexingSocket.Protocol.Internal
{
   internal class IOQueue:IThreadPoolWorkItem
   {

      private readonly ConcurrentQueue<Work> workItems = new ConcurrentQueue<Work>();
      private int doingWork;

      public virtual void Schedule(Func<object?,ValueTask> action, object? state)
      {
         workItems.Enqueue(new Work(action, state));

         // Set working if it wasn't (via atomic Interlocked).
         if (Interlocked.CompareExchange(ref doingWork, 1, 0) == 0)
         {
            // Wasn't working, schedule.
            System.Threading.ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
         }
      }

      void IThreadPoolWorkItem.Execute()
      {
         _ = ExecuteInternal();
      }

      async Task ExecuteInternal()
      {
         while (true)
         {
            while (workItems.TryDequeue(out Work item))
            {
               await item.Callback(item.State);
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

      private readonly struct Work
      {
         public readonly Func<object?,ValueTask>Callback;
         public readonly object? State;

         public Work(Func<object?,ValueTask> callback, object? state)
         {
            Callback = callback;
            State = state;
         }
      }
   }
}
