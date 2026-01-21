using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EasyState.Blazor.Tests;

public class StateComponentBaseTests : BunitContext
{
    public StateComponentBaseTests()
    {
        Services.AddScoped<IAppState, AppState>();
        Services.AddScoped<IEventAggregator, EventAggregator>();
    }

    public class CounterState 
    {
        public int Count { get; set; }
    }

    public class UserState
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    public class TestStateComponent : StateComponentBase
    {
        public CounterState CurrentState => State<CounterState>();
        public UserState CurrentUserState => State<UserState>();

        public void IncrementCounter()
        {
            UpdateState<CounterState>(s => s.Count++);
        }

        public void SetCounter(int value)
        {
            SetState(new CounterState { Count = value });
        }

        public void PublishTestEvent(string message)
        {
            PublishEvent(new TestEvent { Message = message });
        }

        public IDisposable SubscribeToCounterState(Action<CounterState> handler)
        {
            return ObserveState(handler);
        }

        public IDisposable SubscribeToTestEvent(Action<TestEvent> handler)
        {
            return SubscribeEvent(handler);
        }

        public IDisposable SubscribeToTestEventWithPredicate(Action<TestEvent> handler, Func<TestEvent, bool> predicate)
        {
            return SubscribeEvent(handler, predicate);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Count: {CurrentState.Count}");
            builder.CloseElement();
        }
    }

    [Fact]
    public void State_ReturnsCurrentState()
    {
        var cut = Render<TestStateComponent>();

        var state = cut.Instance.CurrentState;

        Assert.NotNull(state);
        Assert.Equal(0, state.Count);
    }

    [Fact]
    public void State_ReturnsSameInstanceForMultipleCalls()
    {
        var cut = Render<TestStateComponent>();

        var state1 = cut.Instance.CurrentState;
        var state2 = cut.Instance.CurrentState;

        Assert.Same(state1, state2);
    }

    [Fact]
    public async Task UpdateState_ModifiesState()
    {
        var cut = Render<TestStateComponent>();

        cut.Instance.IncrementCounter();
        await Task.Delay(50);

        Assert.Equal(1, cut.Instance.CurrentState.Count);
    }

    [Fact]
    public async Task UpdateState_MultipleUpdates_AccumulateChanges()
    {
        var cut = Render<TestStateComponent>();

        for (int i = 0; i < 5; i++)
        {
            cut.Instance.IncrementCounter();
        }
        await Task.Delay(100);

        Assert.Equal(5, cut.Instance.CurrentState.Count);
    }

    [Fact]
    public void SetState_ReplacesState()
    {
        var cut = Render<TestStateComponent>();

        cut.Instance.SetCounter(42);

        Assert.Equal(42, cut.Instance.CurrentState.Count);
    }

    [Fact]
    public void PublishEvent_SendsEventToEventAggregator()
    {
        var mockEventAggregator = new Mock<IEventAggregator>();
        Services.AddSingleton(mockEventAggregator.Object);

        using var ctx = new BunitContext();
        ctx.Services.AddScoped<IAppState, AppState>();
        ctx.Services.AddScoped(_ => mockEventAggregator.Object);
        
        var cut = ctx.Render<TestStateComponent>();

        cut.Instance.PublishTestEvent("Hello");

        mockEventAggregator.Verify(
            ea => ea.Publish(It.Is<TestEvent>(e => e.Message == "Hello")),
            Times.Once);
    }

    [Fact]
    public void SubscribeEvent_ReceivesPublishedEvents()
    {
        var cut = Render<TestStateComponent>();
        TestEvent? receivedEvent = null;
        cut.Instance.SubscribeToTestEvent(e => receivedEvent = e);

        cut.Instance.PublishTestEvent("Test Message");

        Assert.NotNull(receivedEvent);
        Assert.Equal("Test Message", receivedEvent.Message);
    }

    [Fact]
    public void SubscribeEventWithPredicate_FiltersEvents()
    {
        var cut = Render<TestStateComponent>();
        var receivedMessages = new List<string>();
        cut.Instance.SubscribeToTestEventWithPredicate(
            e => receivedMessages.Add(e.Message),
            e => e.Message.StartsWith("Important"));

        cut.Instance.PublishTestEvent("Regular message");
        cut.Instance.PublishTestEvent("Important: Alert!");
        cut.Instance.PublishTestEvent("Another regular message");

        Assert.Single(receivedMessages);
        Assert.Equal("Important: Alert!", receivedMessages[0]);
    }

    [Fact]
    public void ObserveState_ReceivesStateUpdates()
    {
        var cut = Render<TestStateComponent>();
        var receivedStates = new List<CounterState>();
        using var disposable = cut.Instance.SubscribeToCounterState(s => receivedStates.Add(s));

        cut.Instance.SetCounter(10);
        cut.Instance.SetCounter(20);

        Assert.True(receivedStates.Count >= 2);
    }

    [Fact]
    public void Dispose_CleansUpSubscriptions()
    {
        var cut = Render<TestStateComponent>();
        var eventReceived = false;
        cut.Instance.SubscribeToTestEvent(_ => eventReceived = true);

        cut.Instance.Dispose();

        eventReceived = false;

        var cut2 = Render<TestStateComponent>();
        cut2.Instance.PublishTestEvent("After Dispose");

        Assert.False(eventReceived);
    }

    [Fact]
    public void Dispose_MultipleDisposeCalls_DoesNotThrow()
    {
        var cut = Render<TestStateComponent>();
        cut.Instance.SubscribeToTestEvent(_ => { });
        cut.Instance.SubscribeToCounterState(_ => { });

        cut.Instance.Dispose();
        cut.Instance.Dispose();
    }

    [Fact]
    public void Component_SharesStateAcrossInstances()
    {
        var cut1 = Render<TestStateComponent>();
        var cut2 = Render<TestStateComponent>();

        cut1.Instance.SetCounter(100);

        Assert.Equal(100, cut1.Instance.CurrentState.Count);
        Assert.Equal(100, cut2.Instance.CurrentState.Count);
    }

    [Fact]
    public void Component_DifferentStateTypes_AreIsolated()
    {
        var cut = Render<TestStateComponent>();

        cut.Instance.SetCounter(50);

        Assert.Equal(50, cut.Instance.CurrentState.Count);
        Assert.NotNull(cut.Instance.CurrentUserState);
        Assert.Equal(string.Empty, cut.Instance.CurrentUserState.Name);
    }
}
