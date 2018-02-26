using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLazy
{
    /// <summary>
    /// Runs an action only once, it's like Lazy without return.
    /// </summary>
    public class Once
    {
        private Action _action;
        private Func<Task> _asyncAction;
        private bool _didItRun;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Instantiates once, but is expecting action at a later point
        /// </summary>
        public Once()
        {
        }

        /// <summary>
        /// Prepare Once with an action.
        /// </summary>
        public Once(Action action)
        {
            _action = action;
        }

        /// <summary>
        /// Prepare once with an async action
        /// </summary>
        public Once(Func<Task> action)
        {
            _asyncAction = action;
        }
        
        public void Run()
        {
            // shortcut
            if (_didItRun) return;
            // if we're here there is no value.. yet, synchronize!
            var semaphore = _semaphore;
            try
            {
                semaphore.Wait();
                // check again, maybe it was created
                if (_didItRun) return;
                // sync factory
                if (_action != null)
                {
                    _action();
                    // destroy fields that are not needed anymore
                    _action = null;
                }
                // async factory
                if (_asyncAction != null)
                {
                    // yes this blocks the thread, you called the sync version
                    // but it does it on a background thread
                     Task.Run(async () =>
                     {
                         await _asyncAction();
                     }).Wait();
                    // destroy fields that are not needed anymore
                    _asyncAction = null;
                }
                _didItRun = true;
                // destroy fields that are not needed anymore
                _semaphore = null;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task RunAsync()
        {
            // shortcut
            if (_didItRun) return;
            // if we're here there is no valie.. yet
            var semaphore = _semaphore;
            try
            {
                semaphore.Wait();
                // check again, maybe it was created
                if (_didItRun) return;
                if (_action != null)
                {
                    _action();
                    // destroy fields that are not needed anymore
                    _action = null;
                }
                if (_asyncAction != null)
                {
                    // magic, non blocking way of using this
                    await _asyncAction();
                    // destroy fields that are not needed anymore
                    _asyncAction = null;
                }
                _didItRun = true;
                // destroy fields that are not needed anymore
                _semaphore = null;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary> Set if the factory has been called, if set the factory is never called again. </summary>
        public bool DidItRun => _didItRun;
    }
}
