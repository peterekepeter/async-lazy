using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AsyncLazy.AegirWeb.KeyVault;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestUnitForAsyncLazy
{
    [TestClass]
    public class AsyncContextTest
    {
        [TestMethod]
        public void CanRunStuff()
        {
            var context = new AsyncContext();
            var result = context.Run(async () =>
            {
                await Task.Delay(10);
                return 42;
            });
            result.Should().Be(42);
        }
    }
}
