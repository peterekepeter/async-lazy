using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncLazy;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestUnitForAsyncLazy
{
    [TestClass]
    public class AsyncCacheTest
    {
        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, true, false)]
        [DataRow(true, false, false)]
        [DataRow(true, true, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, true)]
        [DataRow(true, false, true)]
        [DataRow(true, true, true)]
        public async Task WillCacheFactoryResult(bool blockingGet, bool asyncFactory, bool useTasks)
        {
            int counter = 0;
            var cache = new AsyncCache<int, int>();
            if (asyncFactory)
            {
                cache.AsyncFactory = async x =>
                {
                    await Task.Delay(1);
                    Interlocked.Increment(ref counter);
                    return x * x;
                };
            }
            else
            {
                cache.Factory = x =>
                {
                    Interlocked.Increment(ref counter);
                    return x * x;
                };
            }

            async Task Job()
            {
                for (int i = 0; i < 10; i++)
                {
                    int value;
                    if (blockingGet)
                    {
                        value = cache.GetValue(i);
                    }
                    else
                    {
                        value = await cache.GetValueAsync(i);
                    }
                    value.Should().Be(i * i);
                }
            }

            if (useTasks)
            {
                var tasks = Enumerable.Range(1, 10).Select(n => Task.Run((Func<Task>) Job));
                await Task.WhenAll(tasks);
            }
            else
            {
                // prepare the threads
                var threads = Enumerable.Range(1, 10).Select(n => new Thread(() => Job().Wait())).ToList();
                // start all the theads
                threads.ForEach(thread => thread.Start());
                threads.ForEach(thread => thread.Join());

            }
            // done? => counter should be 10
            counter.Should().Be(10);
        }

        [TestMethod] 
        public void BackgroundThreadPerformingCleanupShouldNotAlterResults()
        {
            var cache = new AsyncCache<int, int>(x => x * x);
            var threadActive = true;
            var thread = new Thread(() =>{ while (threadActive) { cache.CleanupAsync().Wait(); } });
            thread.Start();
            for (int i=0; i<2000; i++)
            {
                cache.GetValue(i).Should().Be(i * i);
            }
            threadActive = false;
            thread.Join();
        }

        /// <summary>
        /// should easily have 10k / second throughput
        /// </summary>
        [TestMethod]
        public async Task AfterCachingValuesCacheShouldHaveHighMultithreadedPerformance()
        {
            var cache = new AsyncCache<int, int>(x => x * x)
            {
                ItemLimit = 800,
                AutomaticCleanup = false
            };
            // below active count
            // dry run
            Func<int, Task> job = n => Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    cache.GetValue(i%1000);
                }
            });
            // performs optimizations
            cache.OptimizeEvery = TimeSpan.Zero; // after any time passed
            await cache.CleanupAsync();
            Stopwatch stopwatch = new Stopwatch();
            // benchmark
            stopwatch.Start();
            await Task.WhenAll(Enumerable.Range(1, 10).Select(job).ToList());
            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;
            elapsed.Should().BeLessThan(new TimeSpan(0,0,0,1));
        }

        [TestMethod]
        public async Task PurgingShouldNotAffectResults()
        {
            var cache = new AsyncCache<int, int>(x => x * x)
            {
                ItemLimit = 10 // this should force a lot of recycling
            };
            var threadActive = true;
            var thread = new Thread(() => { while (threadActive) { cache.CleanupAsync().Wait(); } });
            thread.Start();
            Func<int, Task> job = n => Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    cache.GetValue(i);
                }
            });
            await Task.WhenAll(Enumerable.Range(1, 10).Select(job).ToList());
            threadActive = false;
            thread.Join();
        }
    }
}
