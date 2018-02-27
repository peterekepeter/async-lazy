using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AsyncLazy;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestUnitForAsyncLazy
{
    [TestClass]
    public class AsyncLockTest
    {
        [TestMethod]
        public void ByDefaultOnlyOneThreadCanEnter()
        {
            int counter = 0;
            var criticalSection = new AsyncLazy.AsyncLock();
            ;
            var threads = Enumerable.Range(1, 10).Select(n => new Thread(() => criticalSection.Run(() =>
            {
                counter++;
                counter.Should().Be(1);
                counter--;
            }))).ToList();
            threads.ForEach(thread => thread.Start());
            threads.ForEach(thread => thread.Join());
            counter.Should().Be(0);
        }
    }
}
