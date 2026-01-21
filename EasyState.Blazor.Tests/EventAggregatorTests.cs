namespace EasyState.Blazor.Tests;

public class EventAggregatorTests : IDisposable
{
    private readonly EventAggregator _eventAggregator;

    public EventAggregatorTests()
    {
        _eventAggregator = new EventAggregator();
    }

    public void Dispose()
    {
        _eventAggregator.Dispose();
    }

    public class TestEvent
    {
        public string Message { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class AnotherTestEvent
    {
        public bool IsSuccess { get; set; }
    }

    [Fact]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        var eventData = new TestEvent { Message = "Test" };

        _eventAggregator.Publish(eventData);
    }

    [Fact]
    public void Publish_WithNullEvent_DoesNotThrow()
    {
        _eventAggregator.Publish<TestEvent>(null!);
    }

    [Fact]
    public void Publish_DeliversEventToSubscriber()
    {
        TestEvent? receivedEvent = null;
        using var subscription = _eventAggregator.SubscribeAction<TestEvent>(e => receivedEvent = e);

        _eventAggregator.Publish(new TestEvent { Message = "Hello", Value = 42 });

        Assert.NotNull(receivedEvent);
        Assert.Equal("Hello", receivedEvent.Message);
        Assert.Equal(42, receivedEvent.Value);
    }

    [Fact]
    public void Publish_DeliversEventToMultipleSubscribers()
    {
        TestEvent? receivedEvent1 = null;
        TestEvent? receivedEvent2 = null;
        using var subscription1 = _eventAggregator.SubscribeAction<TestEvent>(e => receivedEvent1 = e);
        using var subscription2 = _eventAggregator.SubscribeAction<TestEvent>(e => receivedEvent2 = e);

        _eventAggregator.Publish(new TestEvent { Message = "Shared" });

        Assert.NotNull(receivedEvent1);
        Assert.NotNull(receivedEvent2);
        Assert.Equal("Shared", receivedEvent1.Message);
        Assert.Equal("Shared", receivedEvent2.Message);
    }

    [Fact]
    public void Subscribe_ReturnsObservable()
    {
        var observable = _eventAggregator.Subscribe<TestEvent>();

        Assert.NotNull(observable);
    }

    [Fact]
    public void Subscribe_DifferentEventTypes_AreIsolated()
    {
        TestEvent? testEvent = null;
        AnotherTestEvent? anotherEvent = null;
        using var subscription1 = _eventAggregator.SubscribeAction<TestEvent>(e => testEvent = e);
        using var subscription2 = _eventAggregator.SubscribeAction<AnotherTestEvent>(e => anotherEvent = e);

        _eventAggregator.Publish(new TestEvent { Message = "Test" });

        Assert.NotNull(testEvent);
        Assert.Null(anotherEvent);
    }

    [Fact]
    public void Subscribe_MultipleCallsForSameType_ReturnsSameSubject()
    {
        var events1 = new List<TestEvent>();
        var events2 = new List<TestEvent>();

        using var subscription1 = _eventAggregator.Subscribe<TestEvent>().Subscribe(e => events1.Add(e));
        using var subscription2 = _eventAggregator.Subscribe<TestEvent>().Subscribe(e => events2.Add(e));

        _eventAggregator.Publish(new TestEvent { Message = "Event1" });

        Assert.Single(events1);
        Assert.Single(events2);
    }

    [Fact]
    public void SubscribeWithPredicate_FiltersEvents()
    {
        var receivedEvents = new List<TestEvent>();
        using var subscription = _eventAggregator
            .Subscribe<TestEvent>(e => e.Value > 10)
            .Subscribe(e => receivedEvents.Add(e));

        _eventAggregator.Publish(new TestEvent { Message = "Low", Value = 5 });
        _eventAggregator.Publish(new TestEvent { Message = "High", Value = 15 });
        _eventAggregator.Publish(new TestEvent { Message = "Medium", Value = 10 });

        Assert.Single(receivedEvents);
        Assert.Equal("High", receivedEvents[0].Message);
    }

    [Fact]
    public void SubscribeWithPredicate_AllEventsFiltered_NoEventsReceived()
    {
        var receivedEvents = new List<TestEvent>();
        using var subscription = _eventAggregator
            .Subscribe<TestEvent>(e => e.Value > 100)
            .Subscribe(e => receivedEvents.Add(e));

        _eventAggregator.Publish(new TestEvent { Value = 50 });
        _eventAggregator.Publish(new TestEvent { Value = 75 });

        Assert.Empty(receivedEvents);
    }

    [Fact]
    public void SubscribeAction_ExecutesHandler()
    {
        var handlerCalled = false;
        using var subscription = _eventAggregator.SubscribeAction<TestEvent>(_ => handlerCalled = true);

        _eventAggregator.Publish(new TestEvent());

        Assert.True(handlerCalled);
    }

    [Fact]
    public void SubscribeAction_ReturnsDisposable()
    {
        var subscription = _eventAggregator.SubscribeAction<TestEvent>(_ => { });

        Assert.NotNull(subscription);
        subscription.Dispose();
    }

    [Fact]
    public void SubscribeAction_AfterDispose_NoLongerReceivesEvents()
    {
        var callCount = 0;
        var subscription = _eventAggregator.SubscribeAction<TestEvent>(_ => callCount++);

        _eventAggregator.Publish(new TestEvent());
        Assert.Equal(1, callCount);

        subscription.Dispose();
        _eventAggregator.Publish(new TestEvent());

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void SubscribeActionWithPredicate_FiltersEvents()
    {
        var receivedValues = new List<int>();
        using var subscription = _eventAggregator.SubscribeAction<TestEvent>(
            e => receivedValues.Add(e.Value),
            e => e.Value % 2 == 0
        );

        _eventAggregator.Publish(new TestEvent { Value = 1 });
        _eventAggregator.Publish(new TestEvent { Value = 2 });
        _eventAggregator.Publish(new TestEvent { Value = 3 });
        _eventAggregator.Publish(new TestEvent { Value = 4 });

        Assert.Equal(2, receivedValues.Count);
        Assert.Contains(2, receivedValues);
        Assert.Contains(4, receivedValues);
    }

    [Fact]
    public async Task Publish_ConcurrentPublishes_AreThreadSafe()
    {
        var receivedEvents = new List<TestEvent>();
        var lockObj = new object();
        using var subscription = _eventAggregator.SubscribeAction<TestEvent>(e =>
        {
            lock (lockObj)
            {
                receivedEvents.Add(e);
            }
        });

        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            int value = i;
            tasks.Add(Task.Run(() => _eventAggregator.Publish(new TestEvent { Value = value })));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(100, receivedEvents.Count);
    }

    [Fact]
    public async Task Subscribe_ConcurrentSubscriptions_AreThreadSafe()
    {
        var subscriptions = new List<IDisposable>();
        var lockObj = new object();
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var subscription = _eventAggregator.SubscribeAction<TestEvent>(_ => { });
                lock (lockObj)
                {
                    subscriptions.Add(subscription);
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(50, subscriptions.Count);

        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }
    }


    [Fact]
    public void Dispose_MultipleDisposeCalls_DoesNotThrow()
    {
        var eventAggregator = new EventAggregator();
        _ = eventAggregator.Subscribe<TestEvent>();

        eventAggregator.Dispose();
        eventAggregator.Dispose();
    }
}
