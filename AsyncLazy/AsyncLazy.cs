using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLazy
{
    /// <summary> Provides lazy instantiation, good for singleton, uses semaphore so resource is only istantiated once. </summary>
    public class AsyncLazy<Typename>
    {
        private Func<Typename> _factory;
        private Func<Task<Typename>> _asyncFactory;
        private Typename _value;
        private bool _isValueCreated;
        private readonly AsyncLock _lock = new AsyncLock();

        /// <summary>
        /// Create with regular factory.
        /// </summary>
        public AsyncLazy(Func<Typename> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Create lazy with async factory, call GetValueAsync to get value in a non blocking way! You can still use the Value property for sync.
        /// </summary>
        public AsyncLazy(Func<Task<Typename>> factory)
        {
            _asyncFactory = factory;
        }

        /// <summary>
        /// Get value synchronouslym, if factory is async then factory is run in a background task. 
        /// </summary>
        public Typename Value => GetValue();

        /// <summary>
        /// Get value synchronouslym, if factory is async then factory is run in a background task. 
        /// </summary>
        public Typename GetValue()
        {
            // shortcut
            if (_isValueCreated) return _value;
            // if we're here there is no value.. yet, synchronize!
            _lock.Run(()=>
            {
                // check again, maybe it was created
                if (_isValueCreated) return;
                // sync factory
                if (_factory != null)
                {
                    _value = _factory();
                    // destroy fields that are not needed anymore
                    _factory = null;
                }
                // async factory
                if (_asyncFactory != null)
                {
                    // yes this blocks the thread, you called the sync version
                    // but it does it on a background thread
                    _value = Task.Run(async () => await _asyncFactory()).Result;
                    // destroy fields that are not needed anymore
                    _asyncFactory = null;
                }
                _isValueCreated = true;
            });
            return _value;
        }

        /// <summary>
        /// Get value asynchronously, if factory is not async then this will still be synchronous.
        /// </summary>
        public async Task<Typename> GetValueAsync()
        {
            // shortcut
            if (_isValueCreated) return _value;
            // if we're here there is no valie.. yet
            await _lock.RunAsync(async () =>
            {
                // check again, maybe it was created
                if (_isValueCreated) return;
                if (_factory != null)
                {
                    _value = _factory();
                    // destroy fields that are not needed anymore
                    _factory = null;
                }
                if (_asyncFactory != null)
                {
                    // magic, non blocking way of using this
                    _value = await _asyncFactory();
                    // destroy fields that are not needed anymore
                    _asyncFactory = null;
                }
                _isValueCreated = true;
            });
            return _value;
        }

        /// <summary> 
        /// Set if the factory has been called, if set the factory is never called again. 
        /// </summary>
        public bool IsValueCreated => _isValueCreated;
    }
}
