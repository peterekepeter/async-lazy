using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AsyncLazy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    namespace AegirWeb.KeyVault
    {
        /// <summary>
        /// Allows running code in an isolated async context with it's own threads istead of using up threadpool threads.
        /// </summary>
        public class AsyncContext
        {


            private class WorkItem  
            {
                public SendOrPostCallback callback;
                public object state;

                public WorkItem(SendOrPostCallback callback, object state)
                {
                    this.callback = callback;
                    this.state = state;
                }
            }

            ConcurrentQueue<WorkItem> workItems = new ConcurrentQueue<WorkItem>();

            private class AsyncContextSynchronizationContext : SynchronizationContext
            {
                private SemaphoreSlim workSignal;
                private ConcurrentQueue<WorkItem> workItems;

                public AsyncContextSynchronizationContext(SemaphoreSlim semaphoreSlim, ConcurrentQueue<WorkItem> workItems)
                {
                    workSignal = semaphoreSlim;
                    this.workItems = workItems;
                }

                public override SynchronizationContext CreateCopy()
                {
                    return this;
                }

                public override bool Equals(object obj)
                {
                    return obj == this;
                }

                public override int GetHashCode()
                {
                    return base.GetHashCode();
                }

                public override void OperationCompleted()
                {
                    base.OperationCompleted();
                }

                public override void OperationStarted()
                {
                    base.OperationStarted();
                }

                public override void Post(SendOrPostCallback d, object state)
                {
                    workItems.Enqueue(new WorkItem(d, state));
                    workSignal.Release(1);
                }

                public override void Send(SendOrPostCallback d, object state)
                {
                    var semaphore = new SemaphoreSlim(0, 1);
                    workItems.Enqueue(new WorkItem(x =>
                    {
                        d(x);
                        semaphore.Release();
                    }, state));
                    semaphore.Wait();
                }

                public override string ToString()
                {
                    return base.ToString();
                }

                public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
                {
                    return base.Wait(waitHandles, waitAll, millisecondsTimeout);
                }
            }
            
            private Thread[] threads;
            private AsyncContextSynchronizationContext synchronizationContext;
            private static SemaphoreSlim workSignal;

            public AsyncContext(int dedicatedThreadCount = 4)
            {
                workSignal = new SemaphoreSlim(0);
                synchronizationContext = new AsyncContextSynchronizationContext(workSignal, workItems);
                threads = new Thread[dedicatedThreadCount];
                for (int i = 0; i < dedicatedThreadCount; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                        while (true)
                        {
                            workSignal.Wait();
                            if (workItems.TryDequeue(out var item))
                            {
                                item.callback(item.state);
                            }
                        }
                    });
                }
            }

            public void Run(Action action)
            {
                var captured = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                try
                {
                    action();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(captured);
                }
            }

            public TResult Run<TResult>(Func<TResult> action)
            {
                var captured = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                try
                {
                    return action();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(captured);
                }
            }

            public void Run(Func<Task> action)
            {
                var captured = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                try
                {
                    var task = action();
                    task.Wait();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(captured);
                }
            }

            public TResult Run<TResult>(Func<Task<TResult>> action)
            {
                var captured = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                try
                {
                    var task = action();
                    return task.Result;
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(captured);
                }
            }
        }
    }

}
