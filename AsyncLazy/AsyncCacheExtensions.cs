using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AsyncLazy
{
    public static class AsyncCacheExtensions
    {
        public static TValue GetValue<TKey, TValue>(this AsyncCache<TKey, TValue> cache, TKey key, Func<TKey, TValue> factory)
        {
            var options = cache.DefaultCacheCallOptions;
            options.Factory = factory;
            options.AsyncFactory = null;
            return cache.GetValue(key, options);
        }

        public static TValue GetValue<TKey, TValue>(this AsyncCache<TKey, TValue> cache, TKey key, Func<TKey, Task<TValue>> asyncFactory)
        {
            var options = cache.DefaultCacheCallOptions;
            options.Factory = null;
            options.AsyncFactory = asyncFactory;
            return cache.GetValue(key, options);
        }
        public static async Task<TValue> GetValueAsync<TKey, TValue>(this AsyncCache<TKey, TValue> cache, TKey key, Func<TKey, TValue> factory)
        {
            var options = cache.DefaultCacheCallOptions;
            options.Factory = factory;
            options.AsyncFactory = null;
            return await cache.GetValueAsync(key, options);
        }

        public static async Task<TValue> GetValueAsync<TKey, TValue>(this AsyncCache<TKey, TValue> cache, TKey key, Func<TKey, Task<TValue>> asyncFactory)
        {
            var options = cache.DefaultCacheCallOptions;
            options.Factory = null;
            options.AsyncFactory = asyncFactory;
            return await cache.GetValueAsync(key, options);
        }
        
    }
}
