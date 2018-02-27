using System;
using System.Collections.Generic;
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
    public class OnceTest
    {

        [TestMethod]
        public void ActionOnlyCalledOnceInMultitreadedContext()
        {
            int counter = 0;
            var once = new Once(() =>
            {
                Thread.Sleep(10);
                ++counter;
            });
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                once.Run();
            }
            threads.ForEach(thread => thread.Start());
            threads.ForEach(thread => thread.Join());
            counter.Should().Be(1);
        }

        [TestMethod]
        public async Task ActionOnlyCalledOnceInMultiTaskContext()
        {
            int counter = 0;
            var once = new Once(async () =>
            {
                await Task.Delay(10);
                ++counter;
            });
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(once.RunAsync());
            }
            await Task.WhenAll(tasks);
            counter.Should().Be(1);
        }

        [TestMethod]
        public async Task PredicateDeterminesIfOnceRunsAgainAsync()
        {
            bool enabled = false;
            int counter = 0;
            // ReSharper disable once AccessToModifiedClosure
            var once = new Once(() => enabled, async () =>
            {
                await Task.Delay(10);
                enabled = false;
                ++counter;
            });
            await Task.WhenAll(Enumerable.Range(1, 10).Select(n => once.RunAsync()).ToList());
            counter.Should().Be(0);
            enabled = true;
            await Task.WhenAll(Enumerable.Range(1, 10).Select(n => once.RunAsync()).ToList());
            counter.Should().Be(1);
            await Task.WhenAll(Enumerable.Range(1, 10).Select(n => once.RunAsync()).ToList());
            counter.Should().Be(1);
            enabled = true;
            await Task.WhenAll(Enumerable.Range(1, 10).Select(n => once.RunAsync()).ToList());
            counter.Should().Be(2);
        }

        [TestMethod]
        public void PredicateDeterminesIfOnceRunsAgain()
        {
            bool enabled = false;
            int counter = 0;
            // ReSharper disable once AccessToModifiedClosure
            var once = new Once(() => enabled, () =>
            {
                enabled = false;
                ++counter;
            });
            Task.WhenAll(Enumerable.Range(1, 10).Select(n => Task.Run(() => once.Run())).ToList()).Wait();
            counter.Should().Be(0);
            enabled = true;
            Task.WhenAll(Enumerable.Range(1, 10).Select(n => Task.Run(() => once.Run())).ToList()).Wait();
            counter.Should().Be(1);
            Task.WhenAll(Enumerable.Range(1, 10).Select(n => Task.Run(() => once.Run())).ToList()).Wait();
            counter.Should().Be(1);
            enabled = true;
            Task.WhenAll(Enumerable.Range(1, 10).Select(n => Task.Run(() => once.Run())).ToList()).Wait();
            counter.Should().Be(2);
        }

        [TestMethod]
        public void OnceWorksWellWithBackgroundThreads()
        {
            bool threadEnabled = true;
            bool enabled = false;
            int counter = 0;
            // ReSharper disable once AccessToModifiedClosure
            var once = new Once(() => enabled, () =>
            {
                enabled = false;
                ++counter;
            });
            // 10 worker threads
            var threads = Enumerable.Range(1, 10).Select(n => new Thread(() =>
            {
                // ReSharper disable once AccessToModifiedClosure
                while (threadEnabled)
                {
                    once.Run();
                }
            })).ToList();
            threads.ForEach(thread => thread.Start());

            var delay = 20;
            Thread.Sleep(delay);
            counter.Should().Be(0);
            enabled = true;
            Thread.Sleep(delay);
            counter.Should().Be(1);
            Thread.Sleep(delay);
            counter.Should().Be(1);
            enabled = true;
            Thread.Sleep(delay);
            counter.Should().Be(2);

            // done
            threadEnabled = false;
            threads.ForEach(thread => thread.Join());
        }

        [TestMethod]
        public void RanCountShouldBeTheNumberOfTimesRan()
        {
            var enabled = false;
            var counter = 0;
            var once = new Once(() => enabled, () =>
            {
                enabled = false;
                ++counter;
            });
            for (int i=0; i<10; i++)
            {
                enabled = true;
                counter.Should().Be(once.RanCount);
                once.Run();
            }
        }

        [TestMethod]
        public void DidRunShouldBeFalseBeforeFirstRunAndTrueAfter()
        {
            var enabled = false;
            var counter = 0;
            var once = new Once(() => enabled, () =>
            {
                enabled = false;
                ++counter;
            });
            once.DidItRun.Should().Be(false);
            for (int i = 0; i < 10; i++)
            {
                enabled = true;
                once.Run();
                once.DidItRun.Should().Be(true);
            }
        }
    }
}
