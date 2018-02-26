using System.Collections.Generic;
using System.Threading;
using AsyncLazy;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace TestUnitForAsyncLazy
{
    [TestClass]
    public class LazyTest
    {

        [TestMethod]
        public void LazyIsAliasForAsyncLazy()
        {
            var lazy = new AsyncLazy<int>(() => 42);
            lazy.IsValueCreated.Should().BeFalse();
            var value = lazy.Value; // calls the factory
            lazy.IsValueCreated.Should().BeTrue();
        }

        [TestMethod]
        public void CanRunActionsAsWell()
        {
            int sideEffect = 0;
            var lazy = new Lazy(() =>
            {
                sideEffect = 42;
            });
            lazy.IsValueCreated.Should().BeFalse();
            var value = lazy.Value; // calls the factory
            lazy.IsValueCreated.Should().BeTrue();
            sideEffect.Should().Be(42);
        }
    }
}
