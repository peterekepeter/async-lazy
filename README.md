

# AsyncLazy

[![Build Status](https://travis-ci.org/peterekepeter/async-lazy.svg?branch=master)](https://travis-ci.org/peterekepeter/async-lazy)

This is a support library for asynchrounus and lazy execution for your code.
I found that I was in need of these classes multiple times, so I've decided to put them in an open source nuget.

Check out the unit tests to see some examples of what can be done.


## AsyncLazy

This class allows lazy instantiation of stuff. 

	// can use sync or async factory
    var lazy = new AsyncLazy<int>(() => 42);
    var value = await lazy.GetValueAsync(); // can get using sync or async get method
    value.Should().Be(42);

It will make sure that the factory method will only get called once, no matter how many threads or tasks are accessing the same resouce.
	
	int counter = 0;
	var lazy = new AsyncLazy.Lazy<int>(() => ++counter);
	// okay, now start a bunch of tasks in parallel...
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
	// the counter will be 1, no matter what
	counter.Should().Be(1);


## Once

This is a syncronization class, it allows you to set up a process that only runs once. 
It's so useful it's used for implementing the rest of the classes in this library.

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

What it does can be basically done with a lock, but it has one more smart feature.
You can set up a predicate that determines if it needs to be run or not.
Then you can use background threads, or other events or tasks to call the execution.

    bool enabled = false;
    int counter = 0;
	// the first parameter is a predicate that controls execution
    var once = new Once(() => enabled, async () =>
    {
		// and this is the process that needs to be executed
        await Task.Delay(10);
		// put useful code here
        enabled = false;
        ++counter;
    });
	// now we can control process execution with the enabled flag
    await once.RunAsync();
    counter.Should().Be(0);
    await once.RunAsync();
    counter.Should().Be(1);
    await once.RunAsync();
    counter.Should().Be(1);
    enabled = true;
    await once.RunAsync();
    counter.Should().Be(2);
    enabled = true;


## AsyncCache

Is a high preformance caching mechanism with cached item count limit and cache expiration.
In general use it will make minimal locks, inside it optimizies requests into immutable dictionaries
that can be safely accessed on multiple threads. 

It has some important parameters that need to be considered (check out the documentation on each method/property),
but it comes packed with sensible defaults that should be decend out of the box for processes that request about 1000 unique 
items per every 5 minutes.

It can be used with a separate background thread that perofrms stuctural operations on the cache, while
the primary threads return as fast as possible.

Again the same job is never executed twice, even if there are multiple threads requesting the same
resource at the same time, only one threads/task will execute and the rest will await completion.

    var cache = new AsyncCache<int, int>(x => x * x);
    var threadActive = true;
	// a background thread is performing optimizations and cleanup 
    var thread = new Thread(() =>{ while (threadActive) { cache.CleanupAsync().Wait(); } });
    thread.Start();
	// the foreground thread can request as many values as it wants
    for (int i=0; i<2000; i++)
    {
        cache.GetValue(i).Should().Be(i * i);
    }
    threadActive = false;
    thread.Join();

In order to see how well it performs, it allows inspection of the cach misses with extended options.

    var count = 0;
    var cache = new AsyncCache<int, int>(x => x * x);
    var options = new CacheCallOptions
    {
        CacheMissAction = () =>
        {
            count++;
        }
    };
    cache.GetValue(3, options); // call should miss
    await cache.GetValueAsync(3, options); // call should hit
    count.Should().Be(1);


## Questions? Issues? Contributions?

I'm constantly improving this library, create an issue here in github, or fork the repo and add your
own features. As long as it's not breaking existing features and you write tests, it will be accepted.
