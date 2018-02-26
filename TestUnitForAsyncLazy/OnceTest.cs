using System;
using System.Collections.Generic;
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
    }
}
