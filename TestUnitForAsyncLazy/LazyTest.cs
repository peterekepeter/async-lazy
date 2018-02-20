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
        public void CanGetValueOfLazy() 
            => new Lazy<int>(() => 42).Value.Should().Be(42);

        [TestMethod]
        public void FactoryIsOnlyCalledOnce()
        {
            int callCount = 0;
            var lazy = new Lazy<int>(() =>
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
            => new Lazy<int>(async () =>
            {
                await Task.Delay(10);
                return 42;
            })
            .Value.Should().Be(42);

        [TestMethod]
        public void IsValueCreatedIsTrueAfterFactoryIsCalled()
        {
            var lazy = new Lazy<int>(() => 42);
            lazy.IsValueCreated.Should().BeFalse();
            var value = lazy.Value; // calls the factory
            lazy.IsValueCreated.Should().BeTrue();
        }

        [TestMethod]
        public async Task NonBlockingGetValueWorkWithNormalFactory()
        {
            var lazy = new Lazy<int>(() => 42);
            var value = await lazy.GetValueAsync();
            value.Should().Be(42);
        }


        [TestMethod]
        public async Task NonBlockingGetValueWorkWithAsyncFactory()
        {
            var lazy = new Lazy<int>(async () => { await Task.Delay(10); return 42; });
            var value = await lazy.GetValueAsync();
            value.Should().Be(42);
        }
    }
}
