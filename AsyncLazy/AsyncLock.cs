using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLazy
{
    /// <summary>
    /// Allows ony one thread to execute at a time
    /// </summary>
    public class AsyncLock
    {
        private readonly SemaphoreSlim _semaphoreSlim;

        /// <summary>
        /// Allow only a single thread into the section
        /// </summary>
        public AsyncLock() => _semaphoreSlim = new SemaphoreSlim(1,1);
        
        /// <summary>
        /// Run the action in a critical section.
        /// </summary>
        public void Run(Action action)
        {
            _semaphoreSlim.Wait();
            try
            {
                action();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Run the action in a critical section.
        /// </summary>
        public void Run(Func<Task> action)
        {
            _semaphoreSlim.Wait();
            try
            {
                action().Wait();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Run the action in a critical section.
        /// </summary>
        public async Task RunAsync(Action action)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                action();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Run the action in a critical section.
        /// </summary>
        public async Task RunAsync(Func<Task> action)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                await action();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
