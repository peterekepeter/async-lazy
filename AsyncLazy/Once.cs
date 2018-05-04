using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLazy
{
    /// <summary>
    /// Runs an action only once, it's like Lazy without return.
    /// It can optionally have a predicate that determines if it needs to be run,
    /// After running its assumed that the predicate will return false.
    /// </summary>
    public class Once
    {
        private readonly Func<Boolean> _shouldRun;
        private readonly Action _action;
        private readonly Func<Task> _asyncAction;
        private Int32 _ranCount = 0;
        private readonly AsyncLock _lock = new AsyncLock();

        /// <summary>
        /// Instantiates once, but is expecting action at a later point
        /// </summary>
        public Once() => _shouldRun = () => DidItRun == false;

        /// <summary>
        /// Prepare Once with an action.
        /// </summary>
        public Once(Action action) : this() => _action = action;

        /// <summary>
        /// Prepare once with an async action
        /// </summary>
        public Once(Func<Task> action) : this() => _asyncAction = action;
        
        /// <summary>
        /// Prepare once with a should run predicate and action.
        /// </summary>
        public Once(Func<Boolean> shouldRun, Action action) : this(action) => _shouldRun = shouldRun;

        /// <summary>
        /// Prepare once with a should run predicate and an async action.
        /// </summary>
        public Once(Func<Boolean> shouldRun, Func<Task> action) : this(action) => _shouldRun = shouldRun;

        public void Run()
        {
            // shortcut
            if (_shouldRun() == false) return;
            _lock.Run(() =>
            {
                // check again after syncronization
                if (_shouldRun() == false) return;
                // prefer sync acton
                if (_action != null)
                {
                    _action();
                }
                else if (_asyncAction != null)
                {
                    // yes this blocks the thread, you called the sync version
                    // but it does it on a background thread
                    _asyncAction().Wait();
                }
                _ranCount++;
            });
        }

        public async Task RunAsync()
        {
            // shortcut
            if (_shouldRun() == false) return;
            await _lock.RunAsync(async () =>
            {
                // check again, maybe it was created
                if (_shouldRun() == false) return;
                // prefer asnyc acyion
                if (_asyncAction != null)
                {
                    // magic, non blocking way of using this
                    await _asyncAction();
                }
                else if (_action != null)
                {
                    _action();
                }
                _ranCount++;
            });
        }

        /// <summary> Set if the factory has been called, if set the factory is never called again. </summary>
        public bool DidItRun => _ranCount > 0;

        /// <summary>
        /// The number of times the action ran.
        /// </summary>
        public int RanCount => _ranCount;
    }
}
