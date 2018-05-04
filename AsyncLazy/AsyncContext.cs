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
        public class AsyncContext : IDisposable
        {
            /// <summary>
            /// Creates some dedicated threads to run submitted tasks with.
            /// </summary>
            /// <param name="threadCount">The number of dedicated threads to create.</param>
            /// <param name="borrowsCallerThread">The thread which is submitted is borrowed to execute other tasks until the submitted work is done.</param>
            /// <param name="startOnCallerThread">Will start running the submitted work on the thread which submitted it.</param>
            /// <param name="threadName">The name of the dedicated threads, can be used for debug/identification purposes.</param>
            public AsyncContext(
                int threadCount = 4,
                bool borrowsCallerThread = true,
                string threadName = "AsyncContextThread")
            {
                this._borrowsCallerThread = borrowsCallerThread;
                workSignal = threadCount == 0 ? null : new SemaphoreSlim(0);
                synchronizationContext = new AsyncContextSynchronizationContext(workSignal, workItems);
                threads = new Thread[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    var thread = threads[i] = new Thread(ThreadMethod);
                    thread.Name = threadName;
                    thread.Start();
                }
            }

            private void ThreadMethod()
            {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                while (running)
                {
                    workSignal.Wait();
                    DoSomeWork();
                }
            }

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

            void DoSomeWork()
            {
                if (workItems.TryDequeue(out var item))
                {
                    item.callback(item.state);
                }
            }

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
                    workSignal?.Release(1);
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
            private SemaphoreSlim workSignal;
            private Boolean running = true;
            private bool _borrowsCallerThread;
            private bool _startOnCallerThread;


            public void Run(Action action)
            {
                var captured = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                try
                {
                    if (_borrowsCallerThread)
                    {
                        Exception capturedException = null;
                        bool done = false;
                        synchronizationContext.Post(_ =>
                        {
                            try
                            {
                                action();
                            }
                            catch (Exception exception)
                            {
                                capturedException = exception;
                            }
                            finally
                            {
                                done = true;
                            }
                        }, null);
                        while (done == false && _borrowsCallerThread)
                        {
                            DoSomeWork();
                        }
                        if (capturedException != null)
                        {
                            throw capturedException;
                        }
                    }
                    else
                    {
                        action();
                    }
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
                    if (_borrowsCallerThread)
                    {
                        TResult result = default(TResult);
                        Exception capturedException = null;
                        bool done = false;
                        synchronizationContext.Post(_ =>
                        {
                            try
                            {
                                result = action();
                            }
                            catch (Exception exception)
                            {
                                capturedException = exception;
                            }
                            finally
                            {
                                done = true;
                            }
                        }, null);
                        while (done == false && _borrowsCallerThread)
                        {
                            DoSomeWork();
                        }
                        if (capturedException != null)
                        {
                            throw capturedException;
                        }
                        return result;
                    }
                    else
                    {
                        return action();
                    }
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
                    Task task = action();
                    while (_borrowsCallerThread && task.IsCompleted == false)
                    {
                        DoSomeWork();
                    }
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
                    Task<TResult> task = action();
                    while (_borrowsCallerThread && task.IsCompleted == false)
                    {
                        DoSomeWork();
                    }
                    return task.Result;
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(captured);
                }
            }

            public void Finalzie()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (running)
                {
                    running = false;
                    if (threads.Length != 0)
                    {
                        workSignal.Release(threads.Length);
                        foreach (var thread in threads)
                        {
                            thread.Join(100);
                        }
                        workSignal.Dispose();
                    }
                }
            }
        }
    }

}
