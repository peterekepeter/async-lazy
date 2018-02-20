using System;
using System.Threading.Tasks;

namespace AsyncLazy
{
    /// <summary> Provides lazy instantion, good for singleton. </summary>
    public class Lazy<Typename>
    {
        private Func<Typename> _factory;
        private Func<Task<Typename>> _asyncFactory;
        private Typename _value;
        private bool _isValueCreated; 

        public Lazy(Func<Typename> factory)
        {
            _factory = factory;
        }

        public Lazy(Func<Task<Typename>> factory)
        {
            _asyncFactory = factory;
        }
        
        public Typename Value
        {
            get
            {
                // shortcut
                if (_isValueCreated) return _value; 
                // if we're here there is no valie.. yet
                if (_factory != null)
                {
                    _value = _factory();
                }
                if (_asyncFactory != null)
                {
                    // yes this blocks the thread, you called the sync version
                    _value = _asyncFactory().Result;
                }
                _isValueCreated = true;
                return _value;
            }
        }

        public async Task<Typename> GetValueAsync()
        {
            // shortcut
            if (_isValueCreated) return _value;
            // if we're here there is no valie.. yet
            if (_factory != null)
            {
                _value = _factory();
            }
            if (_asyncFactory != null)
            {
                // magic, non blocking way of using this
                _value = await _asyncFactory();
            }
            _isValueCreated = true;
            return _value;
        }

        /// <summary> Set if the factory has been called, if set the factory is never called again. </summary>
        public bool IsValueCreated => _isValueCreated;
    }
}
