using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestUnitForAsyncLazy
{
    [TestClass]
    public class AsnyLazyTest
    {
        [TestMethod]
        public void CanGetValueOfLazy()
            => new AsyncLazy.Lazy<int>(() => 42).Value.Should().Be(42);

        [TestMethod]
        public void FactoryIsOnlyCalledOnce()
        {
            int callCount = 0;
            var lazy = new AsyncLazy.Lazy<int>(() =>
            {
                callCount++;
                return 42;
            });
            callCount.Should().Be(0);
            lazy.Value.Should().Be(42);
            callCount.Should().Be(1);
            lazy.Value.Should().Be(42);
            callCount.Should().Be(1);
        }

        [TestMethod]
        public void AsyncFactoryAlsoWorks()
            => new AsyncLazy.Lazy<int>(async () =>
            {
                await Task.Delay(10);
                return 42;
            })
            .Value.Should().Be(42);

        [TestMethod]
        public void IsValueCreatedIsTrueAfterFactoryIsCalled()
        {
            var lazy = new AsyncLazy.Lazy<int>(() => 42);
            lazy.IsValueCreated.Should().BeFalse();
            var value = lazy.Value; // calls the factory
            lazy.IsValueCreated.Should().BeTrue();
        }

        [TestMethod]
        public async Task NonBlockingGetValueWorkWithNormalFactory()
        {
            var lazy = new AsyncLazy.Lazy<int>(() => 42);
            var value = await lazy.GetValueAsync();
            value.Should().Be(42);
        }


        [TestMethod]
        public async Task NonBlockingGetValueWorkWithAsyncFactory()
        {
            var lazy = new AsyncLazy.Lazy<int>(async () => { await Task.Delay(10); return 42; });
            var value = await lazy.GetValueAsync();
            value.Should().Be(42);
        }

        [TestMethod]
        public void FactoryOnlyCalledOnceInMultitreadedContext()
        {
            int counter = 0;
            var lazy = new AsyncLazy.Lazy<int>(() => ++counter);
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                threads.Add(new Thread(() =>
                {
                    Thread.Sleep(10);
                    lazy.Value.Should().Be(1);
                }));
            }
            threads.ForEach(thread => thread.Start());
            threads.ForEach(thread => thread.Join());
            counter.Should().Be(1);
        }

        [TestMethod]
        public async Task FactoryOnlyCalledOnceInMultiTaskContext()
        {
            int counter = 0;
            var lazy = new AsyncLazy.Lazy<int>(() => ++counter);
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(10);
                    return (await lazy.GetValueAsync()).Should().Be(1);
                }));
            }
            await Task.WhenAll(tasks);
        }
    }
}
