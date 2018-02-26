using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLazy
{
    /// <summary>
    /// An alias for AsynLazy, so that people can use it as a drop in replacement for Lazy.
    /// Recommended to use AsyncLazy instead to be more specific, and not to mistake with regular Lazy.
    /// </summary>
    public class Lazy<Typename> : AsyncLazy<Typename>
    {
        public Lazy(Func<Typename> factory) : base(factory)
        {
        }

        public Lazy(Func<Task<Typename>> factory) : base(factory)
        {
        }
    }

    /// <summary>
    /// An alias for Once, because lazy with return values is basically that. It also adats the Once interface to Lazy's interface.
    /// Recommended to use Once instead to be more specific.
    /// </summary>
    public class Lazy : Once
    {
        public Lazy()
        {
        }

        public Lazy(Action action) : base(action)
        {
        }

        public Lazy(Func<Task> action) : base(action)
        {
        }

        public Boolean Value
        {
            get
            {
                Run();
                return true;
            }
        }

        public async Task<Boolean> GetValueAsync()
        {
            await RunAsync();
            return true;
        }

        public bool IsValueCreated => DidItRun;
    }
}
