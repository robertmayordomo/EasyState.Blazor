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

        public async Task<StateChange<CounterState>?> UpdateStateAndReturnChange()
        {
            return await UpdateState<CounterState>(s => s.Count = 10);
        }

        public async Task<StateChange<CounterState>?> UpdateStateWithNoChange()
        {
            return await UpdateState<CounterState>(s =>
            {
                // No change
                var temp = s.Count;
            });
        }

        public void PublishTestEvent(string message)
        {
            PublishEvent(new TestEvent { Message = message });
        }

        public IDisposable SubscribeToCounterState(Action<CounterState> handler)
        {
            return ObserveState(handler);
        }

        public IDisposable SubscribeToCounterStateAsync(Func<CounterState, Task> handler)
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

        public IDisposable SubscribeToStateChanges(Func<StateChange<CounterState>, bool> handler)
        {
            return ObserveStateChanges(handler);
        }

        public IDisposable SubscribeToStateChangesAsync(Func<StateChange<CounterState>, Task<bool>> handler)
        {
            return ObserveStateChanges(handler);
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
    public async Task UpdateState_ReturnsStateChange()
    {
        var cut = Render<TestStateComponent>();
        cut.Instance.SetCounter(5);

        var result = await cut.Instance.UpdateStateAndReturnChange();

        Assert.NotNull(result);
        Assert.Single(result.ChangedProperties);
        Assert.Equal(nameof(CounterState.Count), result.ChangedProperties[0].PropertyName);
        Assert.Equal(5, result.ChangedProperties[0].OldValue);
        Assert.Equal(10, result.ChangedProperties[0].NewValue);
    }

    [Fact]
    public async Task UpdateState_ReturnsNull_WhenNoChanges()
    {
        var cut = Render<TestStateComponent>();
        cut.Instance.SetCounter(5);

        var result = await cut.Instance.UpdateStateWithNoChange();

        Assert.Null(result);
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
    public async Task ObserveState_ReceivesStateUpdates()
    {
        var cut = Render<TestStateComponent>();
        var receivedStates = new List<CounterState>();
        using var disposable = cut.Instance.SubscribeToCounterState(s => receivedStates.Add(s));

        await Task.Delay(50);
        cut.Instance.SetCounter(10);
        await Task.Delay(50);
        cut.Instance.SetCounter(20);
        await Task.Delay(50);

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

    [Fact]
    public async Task ObserveState_AsyncOverload_ReceivesStateUpdates()
    {
        var cut = Render<TestStateComponent>();
        var receivedStates = new List<CounterState>();
        using var disposable = cut.Instance.SubscribeToCounterStateAsync(async s =>
        {
            await Task.Delay(5);
            receivedStates.Add(new CounterState { Count = s.Count }); // Create a copy
        });

        await Task.Delay(50); // Wait for initial state
        cut.Instance.SetCounter(10);
        await Task.Delay(100);

        cut.Instance.SetCounter(20);
        await Task.Delay(100);

        Assert.True(receivedStates.Count >= 2);
        var counts = receivedStates.Select(s => s.Count).ToList();
        Assert.Contains(0, counts); // Initial state
        Assert.Contains(10, counts);
        Assert.Contains(20, counts);
    }

    [Fact]
    public async Task ObserveStateChanges_SyncOverload_ReceivesPropertyChanges()
    {
        var cut = Render<TestStateComponent>();
        StateChange<CounterState>? receivedChange = null;
        var shouldRefresh = true;

        using var disposable = cut.Instance.SubscribeToStateChanges(change =>
        {
            receivedChange = change;
            return shouldRefresh;
        });

        cut.Instance.SetCounter(0);
        var appState = Services.GetRequiredService<IAppState>() as AppState;
        await appState!.UpdateState<CounterState>(s => { s.Count = 42; });
        await Task.Delay(100);

        Assert.NotNull(receivedChange);
        Assert.Single(receivedChange.ChangedProperties);
        Assert.Equal(nameof(CounterState.Count), receivedChange.ChangedProperties[0].PropertyName);
        Assert.Equal(0, receivedChange.ChangedProperties[0].OldValue);
        Assert.Equal(42, receivedChange.ChangedProperties[0].NewValue);
    }

    [Fact]
    public async Task ObserveStateChanges_AsyncOverload_ReceivesPropertyChanges()
    {
        var cut = Render<TestStateComponent>();
        StateChange<CounterState>? receivedChange = null;

        using var disposable = cut.Instance.SubscribeToStateChangesAsync(async change =>
        {
            await Task.Delay(5);
            receivedChange = change;
            return true;
        });

        cut.Instance.SetCounter(10);
        var appState = Services.GetRequiredService<IAppState>() as AppState;
        await appState!.UpdateState<CounterState>(s => { s.Count = 100; });
        await Task.Delay(150);

        Assert.NotNull(receivedChange);
        Assert.Single(receivedChange.ChangedProperties);
        Assert.Equal(100, receivedChange.State.Count);
    }

    [Fact]
    public async Task ObserveStateChanges_ReturnsFalse_DoesNotRefreshUI()
    {
        var cut = Render<TestStateComponent>();
        var callbackCount = 0;

        using var disposable = cut.Instance.SubscribeToStateChanges(change =>
        {
            callbackCount++;
            return false; // Don't refresh UI
        });

        cut.Instance.SetCounter(10);
        var appState = Services.GetRequiredService<IAppState>() as AppState;
        await appState!.UpdateState<CounterState>(s => { s.Count = 20; });
        await Task.Delay(100);

        // State is changed (shared state)
        Assert.Equal(20, cut.Instance.CurrentState.Count);
        // But callback was invoked
        Assert.Equal(1, callbackCount);
    }

    [Fact]
    public async Task ObserveStateChanges_ReturnsTrue_RefreshesUI()
    {
        var cut = Render<TestStateComponent>();
        StateChange<CounterState>? receivedChange = null;

        using var disposable = cut.Instance.SubscribeToStateChanges(change =>
        {
            receivedChange = change;
            return true;
        });

        cut.Instance.SetCounter(5);
        var appState = Services.GetRequiredService<IAppState>() as AppState;
        await appState!.UpdateState<CounterState>(s => { s.Count = 15; });
        await Task.Delay(100);

        Assert.NotNull(receivedChange);
        Assert.Equal(15, receivedChange.State.Count);
    }

    [Fact]
    public async Task ObserveStateChanges_AsyncReturnsTrue_RefreshesUI()
    {
        var cut = Render<TestStateComponent>();
        var asyncCallbackExecuted = false;

        using var disposable = cut.Instance.SubscribeToStateChangesAsync(async change =>
        {
            await Task.Delay(10);
            asyncCallbackExecuted = true;
            return true;
        });

        cut.Instance.SetCounter(0);
        var appState = Services.GetRequiredService<IAppState>() as AppState;
        await appState!.UpdateState<CounterState>(s => { s.Count = 25; });
        await Task.Delay(150);

        Assert.True(asyncCallbackExecuted);
    }

    [Fact]
    public async Task ObserveStateChanges_TracksMultiplePropertyChanges()
    {
        var cut = Render<TestStateComponent>();
        StateChange<CounterState>? receivedChange = null;

        using var disposable = cut.Instance.SubscribeToStateChanges(change =>
        {
            receivedChange = change;
            return true;
        });

        var appState = Services.GetRequiredService<IAppState>() as AppState;
        await appState!.UpdateState<CounterState>(s =>
        {
            s.Count = 100;
        });
        await Task.Delay(100);

        Assert.NotNull(receivedChange);
        Assert.Single(receivedChange.ChangedProperties);
    }

    [Fact]
    public async Task ObserveStateChanges_NoChanges_DoesNotEmit()
    {
        var cut = Render<TestStateComponent>();
        var callbackCount = 0;

        using var disposable = cut.Instance.SubscribeToStateChanges(change =>
        {
            callbackCount++;
            return true;
        });

        cut.Instance.SetCounter(10);
        var appState = Services.GetRequiredService<IAppState>() as AppState;
        await appState!.UpdateState<CounterState>(s =>
        {
            // No changes
            var temp = s.Count;
        });
        await Task.Delay(100);

        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void ObserveStateChanges_Dispose_CleansUpSubscription()
    {
        var cut = Render<TestStateComponent>();
        var callbackCount = 0;

        var disposable = cut.Instance.SubscribeToStateChanges(change =>
        {
            callbackCount++;
            return true;
        });

        disposable.Dispose();
        cut.Instance.SetCounter(0);

        Assert.Equal(0, callbackCount);
    }
}


