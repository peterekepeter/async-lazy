using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncLazy.AsyncLazy;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestUnitForAsyncLazy
{
    [TestClass]
    public class AsyncContextTest
    {
        [TestMethod]
        [DataRow(false, 4)]
        [DataRow(true, 4)]
        [DataRow(true, 0)]
        public void CanRunFunctionAsync(bool borrow, int count)
        {
            using (var context = new AsyncContext(borrowsCallerThread: borrow, threadCount: count))
            {
                var result = context.Run(async () =>
                {
                    await Task.Delay(100);
                    return 42;
                });
                result.Should().Be(42);
            }
        }

        [TestMethod]
        [DataRow(false, 4)]
        [DataRow(true, 4)]
        [DataRow(true, 0)]
        public void CanRunActionAsync(bool borrow, int count)
        {
            using (var context = new AsyncContext(borrowsCallerThread: borrow, threadCount: count))
            {
                var result = 0;
                context.Run(async () =>
                {
                    await Task.Delay(100);
                    result = 42;
                });
                result.Should().Be(42);
            }
        }

        [TestMethod]
        [DataRow(false, 4)]
        [DataRow(true, 4)]
        [DataRow(true, 0)]
        public void CanRunFunction(bool borrow, int count)
        {
            using (var context = new AsyncContext(borrowsCallerThread: borrow, threadCount: count))
            {
                var result = context.Run(() =>
                {
                    Task.Delay(100).Wait();
                    return 42;
                });
                result.Should().Be(42);
            }
        }

        [TestMethod]
        [DataRow(false, 4)]
        [DataRow(true, 4)]
        [DataRow(true, 0)]
        public void CanRunAction(bool borrow, int count)
        {
            using (var context = new AsyncContext(borrowsCallerThread: borrow, threadCount: count))
            {
                var result = 0;
                context.Run(() =>
                {
                    Task.Delay(100).Wait();
                    result = 42;
                });
                result.Should().Be(42);
            }
        }

        [TestMethod]
        public void TasksAreScheduledOnCustomThreads()
        {
            using (var context = new AsyncContext(4, threadName: "Test"))
            {
                context.Run(() =>
                {
                    async Task Func()
                    {
                        await Task.Delay(100);
                        var thread = Thread.CurrentThread;
                        thread.Name.Should().Be("Test");
                        Thread.CurrentThread.IsThreadPoolThread.Should().BeFalse();
                    }
                    Func().Wait();
                });
            }
        }
    }
}
