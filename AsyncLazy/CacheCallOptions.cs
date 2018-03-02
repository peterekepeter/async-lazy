using System;

namespace AsyncLazy
{
    /// <summary> Allows fine-er cache control on a per request basis. Useful for statistics as well. </summary>
    public class CacheCallOptions
    {
        /// <summary> By defaulc false, that's the whole point of the cache, but you can control this using this flag. </summary>
        public bool DontCallFactory;

        /// <summary> If not null then this function is called if there is a cache miss. Useful for statistics. This is invoked before the factory.
        /// The action is only called by one thread at a time, but it should be a short decision as it will block other threads from accessing parts of the cache. </summary>
        public Action CacheMissAction;

        /// <summary> The default cache call options. </summary>
        public static CacheCallOptions GetDefaultOptions() => new CacheCallOptions()
        {
            DontCallFactory = false,
            CacheMissAction = null
        };
    }
    
}