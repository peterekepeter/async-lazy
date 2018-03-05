using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLazy
{

    /// <summary>
    /// Allows efficient multi threaded caching through non blocking API.
    /// </summary>
    /// <typeparam name="TKey">Should uniquely identify the value.</typeparam>
    /// <typeparam name="TValue">The object that needs to be cached.</typeparam>
    public class AsyncCache<TKey, TValue>
    {
        private Dictionary<TKey, AsyncLazy<TValue>> _hotBucket;
        private ImmutableDictionary<TKey, TValue> _warmBucket;
        private ImmutableDictionary<TKey, TValue> _coldBucket;
        private Func<TKey, TValue> _factory;
        private Func<TKey, Task<TValue>> _asyncFactory;
        private readonly AsyncLock _hotBucketLock = new AsyncLock();
        private readonly Once _cleanupProcess;
        private DateTime _lastPurge;
        private DateTime _lastOptimization;
        private Boolean _automaticCleanup = true;
        private Boolean _isCleanupRunning = false;
        private TimeSpan _optimizeEvery = new TimeSpan(0, 1, 0);
        private TimeSpan _purgeEvery = new TimeSpan(0, 5, 0);
        private Int32 _itemLimit = 1000;
        private Int32 _hotItemCount = 0;
        private CacheCallOptions<TKey, TValue> _defaultCacheCallOptions;

        /// <summary>
        /// You must set either Factory or AsyncFactory after initializing with default constructor before usage.
        /// </summary>
        public AsyncCache()
        {
            _hotBucket = new Dictionary<TKey, AsyncLazy<TValue>>();
            var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();
            _warmBucket = _coldBucket = builder.ToImmutable();
            _lastOptimization = _lastPurge = DateTime.Now;
            _hotItemCount = 0;
            _defaultCacheCallOptions = CacheCallOptions<TKey, TValue>.GetDefaultOptions();
            _cleanupProcess = new Once(() =>
            {
                var now = DateTime.Now;
                return _isCleanupRunning == false && (NeedToOptimize(now) || NeedToPurge(now));
            }, CleanupImplementation);
        }

        /// <summary>
        /// Auto initliazizes AsyncCache with a factory
        /// </summary>
        public AsyncCache(Func<TKey, TValue> factory) : this() => Factory = factory;

        /// <summary>
        /// Auto initliazizes AsyncCache with a factory
        /// </summary>
        public AsyncCache(Func<TKey, Task<TValue>> asyncFactory) : this() => AsyncFactory = asyncFactory;
        
        /// <summary>
        /// The cache will re-optimize itself after every TimeSpan passes, optimization places items into an immutable structure 
        /// that allows multithreaded reading without blocking.
        /// </summary>
        public TimeSpan OptimizeEvery
        {
            get { return _optimizeEvery; }
            set { _optimizeEvery = value; }
        }

        /// <summary>
        /// Sets the time limit between purgess. Items may survive a couple of purges, but after 3 times it's definitely gone. 
        /// </summary>
        public TimeSpan PurgeEvery
        {
            get { return _purgeEvery; }
            set { _purgeEvery = value; }
        }
        
        /// <summary>
        /// Sets up an factory. Should produce the same way as AsyncFactory but only one needs to be configured.
        /// </summary>
        public Func<TKey, TValue> Factory
        {
            get { return _factory; }
            set { _factory = value; }
        }

        /// <summary>
        /// Sets up an async factory. Should produce the same way as Factory but only one needs to be configured.
        /// </summary>
        public Func<TKey, Task<TValue>> AsyncFactory
        {
            get { return _asyncFactory; }
            set { _asyncFactory = value; }
        }
        
        /// <summary>
        /// By default true, and if set to false then you need to manually call cleanup once in a while.
        /// </summary>
        public bool AutomaticCleanup
        {
            get { return _automaticCleanup; }
            set { _automaticCleanup = value; }
        }


        /// <summary> By deafult 1000, sets the maximum items that can be kept in a bucket. The cache may hold more items, but this is the mimimum it will. </summary>
        public int ItemLimit
        {
            get { return _itemLimit; }
            set { _itemLimit = value; }
        }


        /// <summary> The default cache call options, used in all requests that dont explicityle specify the options. Cannot be set to null, when attempting to set to null it will default to CacheCallOptions.GetDefaultOptions() </summary>
        public CacheCallOptions<TKey, TValue> DefaultCacheCallOptions
        {
            set => _defaultCacheCallOptions = value ?? CacheCallOptions<TKey, TValue>.GetDefaultOptions();
            get => _defaultCacheCallOptions;
        }

        /// <summary> If value has been created it's returned immediately, otherwise the factory is called. </summary>
        public TValue GetValue(TKey key)
        {
            Task cleanupTask = AutomaticCleanup ? Task.Run(async () => await CleanupAsync()) : null;
            var result = ActuallyGetValueOrCallFactory(key, _defaultCacheCallOptions);
            cleanupTask?.Wait();
            return result;
        }

        /// <summary> If value has been created it's returned immediately, otherwise the factory is called. </summary>
        public TValue GetValue(TKey key, CacheCallOptions<TKey, TValue> options) 
        {
            Task cleanupTask = AutomaticCleanup ? Task.Run(async () => await CleanupAsync()) : null;
            var result = ActuallyGetValueOrCallFactory(key, options);
            cleanupTask?.Wait();
            return result;
        }
        
        /// <summary> If value has been created it's returned immediately, otherwise the factory is called, all this in a non blocking way. </summary>
        public async Task<TValue> GetValueAsync(TKey key)
        {
            Task cleanupTask = AutomaticCleanup ? CleanupAsync() : null;
            var result = GetValueAsync(key, _defaultCacheCallOptions);
            if (cleanupTask != null) await cleanupTask;
            return await result;
        }
        
        /// <summary> If value has been created it's returned immediately, otherwise the factory is called, all this in a non blocking way. </summary>
        public async Task<TValue> GetValueAsync(TKey key, CacheCallOptions<TKey, TValue> options)
        {
            Task cleanupTask = AutomaticCleanup ? CleanupAsync() : null;
            var result = ActuallyGetValueOrCallFactoryAsync(key, options);
            if (cleanupTask != null) await cleanupTask;
            return await result;
        }

        /// <summary>
        /// If cache is not set up for automatic cleanup, this should be called once in a while,
        /// it can be called from a background thread so retrieving from cache in a foreground thread is fast as possible.
        /// </summary>
        public async Task CleanupAsync()
        {
            await _cleanupProcess.RunAsync();
        }

        private bool NeedToPurge(DateTime now) => _hotItemCount > ItemLimit || now - _lastPurge > PurgeEvery;

        private bool NeedToOptimize(DateTime now) => now - _lastOptimization > OptimizeEvery;

        private async Task CleanupImplementation()
        {
            _isCleanupRunning = true;
            // do the cleanup
            var now = DateTime.UtcNow;
            var needToPurge = NeedToPurge(now);
            var needToOptimize = NeedToOptimize(now);
            var hotCount = 0;
            // build new immutable dictionary
            Dictionary<TKey, AsyncLazy<TValue>> oldHotBucket = null;
            await _hotBucketLock.RunAsync(() =>
            {
                hotCount = _hotBucket.Count;
                if (hotCount > 0)
                {
                    oldHotBucket = _hotBucket;
                    _hotBucket = new Dictionary<TKey, AsyncLazy<TValue>>();
                }
            });
            // check if items would overflow
            if (needToOptimize && hotCount + _warmBucket.Count > ItemLimit)
            {
                needToPurge = true;
            }
            // if we're not purgning and _hotBucket was empty
            if (oldHotBucket == null && needToPurge == false)
            {
                _isCleanupRunning = false;
                return;// no need to do anything
            }
            var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();
            // oldHotBucket is null if there is no need to regenerate
            if (oldHotBucket != null)
            {
                foreach (var entry in oldHotBucket)
                {
                    builder.TryAdd(entry.Key, await entry.Value.GetValueAsync());
                }
            }
            // populate with _warmBucket as well if optimize only
            if (needToPurge == false && needToOptimize == true)
            {
                foreach(var entry in _warmBucket)
                {
                    builder.TryAdd(entry.Key, entry.Value);
                }
            }
            var _newBucket = builder.ToImmutable();
            if (needToPurge)
            {
                // during purge, we throw out the cold bucket
                _coldBucket = _warmBucket;
                _warmBucket = _newBucket;
                _lastPurge = _lastOptimization = now;
            }
            else
            {
                // during optimization a warm bucket is replaced with new warm bucket
                _warmBucket = _newBucket;
                _lastOptimization = now;
            }
            // cleanup done
            _isCleanupRunning = false;
        }

        private TValue ActuallyGetValueOrCallFactory(TKey key, CacheCallOptions<TKey, TValue> options)
        {
            if (_coldBucket.TryGetValue(key, out var result))
            {
                return result;
            }
            if (_warmBucket.TryGetValue(key, out result))
            {
                return result;
            }
            // syncronize across threads
            _hotBucketLock.Run(() =>
            {
                if (_hotBucket.TryGetValue(key, out var lazyResult))
                {
                    result = lazyResult.GetValue();
                }
                else
                {
                    // treat cache call options
                    options.CacheMissAction?.Invoke();
                    if (options.DontCallFactory)
                    {
                        result = default(TValue);
                        return;
                    }
                    var asyncFactory = options.AsyncFactory ?? AsyncFactory;
                    var factory = options.Factory ?? Factory;
                    AsyncLazy<TValue> factoryCall = null;
                    if (Factory != null)
                    {
                        factoryCall = new AsyncLazy<TValue>(() => factory(key));
                    }
                    else if (AsyncFactory != null)
                    {
                        factoryCall = new AsyncLazy<TValue>(async () => await asyncFactory(key));
                    }
                    else
                    {
                        throw new InvalidOperationException("Misconfigured AsyncCache: there is no factory.");
                    }
                    _hotBucket.Add(key, factoryCall);
                    _hotItemCount = _hotBucket.Count;
                    result = factoryCall.GetValue();
                }
            });
            return result;
        }
        
        private async Task<TValue> ActuallyGetValueOrCallFactoryAsync(TKey key, CacheCallOptions<TKey, TValue> options)
        {
            if (_coldBucket.TryGetValue(key, out var result))
            {
                return result;
            }
            if (_warmBucket.TryGetValue(key, out result))
            {
                return result;
            }
            // syncronize across threads
            await _hotBucketLock.RunAsync(async () =>
            {
                if (_hotBucket.TryGetValue(key, out var lazyResult))
                {
                    result = lazyResult.GetValue();
                }
                else
                {
                    // treat cache call options
                    options.CacheMissAction?.Invoke();
                    if (options.DontCallFactory)
                    {
                        result = default(TValue);
                        return;
                    }
                    var asyncFactory = options.AsyncFactory ?? AsyncFactory;
                    var factory = options.Factory ?? Factory;
                    AsyncLazy<TValue> factoryCall = null;
                    if (asyncFactory != null)
                    {
                        factoryCall = new AsyncLazy<TValue>(async () => await asyncFactory(key));
                    }
                    else if (factory != null)
                    {
                        factoryCall = new AsyncLazy<TValue>(() => factory(key));
                    }
                    else
                    {
                        throw new InvalidOperationException("Misconfigured AsyncCache: there is no factory.");
                    }
                    _hotBucket.Add(key, factoryCall);
                    _hotItemCount = _hotBucket.Count;
                    result = await factoryCall.GetValueAsync();
                }
            });
            return result;
        }
    }
}
