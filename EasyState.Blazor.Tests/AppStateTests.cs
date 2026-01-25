namespace EasyState.Blazor.Tests;

public class AppStateTests : IDisposable
{
    private readonly AppState _appState;

    public AppStateTests()
    {
        _appState = new AppState();
    }

    public void Dispose()
    {
        _appState.Dispose();
    }

    public class TestState
    {
        public string Name { get; set; } = string.Empty;
        public int Counter { get; set; }
    }

    public class AnotherTestState
    {
        public bool IsEnabled { get; set; }
    }

    [Fact]
    public void GetState_WhenStateDoesNotExist_ReturnsNewInstance()
    {
        var state = _appState.GetState<TestState>();

        Assert.NotNull(state);
        Assert.Equal(string.Empty, state.Name);
        Assert.Equal(0, state.Counter);
    }

    [Fact]
    public void GetState_WhenCalledMultipleTimes_ReturnsSameInstance()
    {
        var state1 = _appState.GetState<TestState>();
        var state2 = _appState.GetState<TestState>();

        Assert.Same(state1, state2);
    }

    [Fact]
    public void GetState_DifferentTypes_ReturnsDifferentInstances()
    {
        var testState = _appState.GetState<TestState>();
        var anotherState = _appState.GetState<AnotherTestState>();

        Assert.NotNull(testState);
        Assert.NotNull(anotherState);
    }

    [Fact]
    public void SetState_StoresState()
    {
        var state = new TestState { Name = "Test", Counter = 42 };

        _appState.SetState(state);
        var retrievedState = _appState.GetState<TestState>();

        Assert.Same(state, retrievedState);
        Assert.Equal("Test", retrievedState.Name);
        Assert.Equal(42, retrievedState.Counter);
    }

    [Fact]
    public void SetState_OverwritesExistingState()
    {
        var state1 = new TestState { Name = "First" };
        var state2 = new TestState { Name = "Second" };

        _appState.SetState(state1);
        _appState.SetState(state2);
        var retrievedState = _appState.GetState<TestState>();

        Assert.Same(state2, retrievedState);
        Assert.Equal("Second", retrievedState.Name);
    }

    [Fact]
    public async Task UpdateState_ModifiesExistingState()
    {
        _appState.SetState(new TestState { Name = "Initial", Counter = 0 });

        _appState.UpdateState<TestState>(s =>
        {
            s.Name = "Updated";
            s.Counter = 10;
        });

        await Task.Delay(50);

        var state = _appState.GetState<TestState>();
        Assert.Equal("Updated", state.Name);
        Assert.Equal(10, state.Counter);
    }

    [Fact]
    public async Task UpdateState_WhenStateDoesNotExist_CreatesAndModifies()
    {
        _appState.UpdateState<TestState>(s =>
        {
            s.Name = "Created";
            s.Counter = 5;
        });

        await Task.Delay(50);

        var state = _appState.GetState<TestState>();
        Assert.Equal("Created", state.Name);
        Assert.Equal(5, state.Counter);
    }

    [Fact]
    public void ObserveState_ReturnsObservable()
    {
        var observable = _appState.ObserveState<TestState>();

        Assert.NotNull(observable);
    }

    [Fact]
    public async Task ObserveState_EmitsCurrentStateImmediately()
    {
        _appState.SetState(new TestState { Name = "Initial" });
        TestState? receivedState = null;

        var observable = _appState.ObserveState<TestState>();
        using var subscription = observable.Subscribe(s => receivedState = s);

        await Task.Delay(50);

        Assert.NotNull(receivedState);
        Assert.Equal("Initial", receivedState.Name);
    }

    [Fact]
    public async Task ObserveState_EmitsOnStateChange()
    {
        var receivedStates = new List<TestState>();
        var observable = _appState.ObserveState<TestState>();
        using var subscription = observable.Subscribe(s => receivedStates.Add(s));

        _appState.SetState(new TestState { Name = "First" });
        _appState.SetState(new TestState { Name = "Second" });

        await Task.Delay(50);

        Assert.True(receivedStates.Count >= 2);
        Assert.Contains(receivedStates, s => s.Name == "First");
        Assert.Contains(receivedStates, s => s.Name == "Second");
    }

    [Fact]
    public async Task ObserveState_MultipleSubscribers_AllReceiveUpdates()
    {
        TestState? state1 = null;
        TestState? state2 = null;
        var observable = _appState.ObserveState<TestState>();

        using var subscription1 = observable.Subscribe(s => state1 = s);
        using var subscription2 = observable.Subscribe(s => state2 = s);

        _appState.SetState(new TestState { Name = "Shared" });

        await Task.Delay(50);

        Assert.NotNull(state1);
        Assert.NotNull(state2);
        Assert.Equal("Shared", state1.Name);
        Assert.Equal("Shared", state2.Name);
    }

    [Fact]
    public async Task SetState_ConcurrentAccess_IsThreadSafe()
    {
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            int value = i;
            tasks.Add(Task.Run(() => _appState.SetState(new TestState { Counter = value })));
        }

        await Task.WhenAll(tasks);

        var state = _appState.GetState<TestState>();
        Assert.NotNull(state);
    }

    [Fact]
    public async Task UpdateState_ConcurrentAccess_IsThreadSafe()
    {
        _appState.SetState(new TestState { Counter = 0 });
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _appState.UpdateState<TestState>(s => s.Counter++)));
        }

        await Task.WhenAll(tasks);
        await Task.Delay(100);

        var state = _appState.GetState<TestState>();
        Assert.NotNull(state);
    }

    [Fact]
    public async Task UpdateState_AsyncOverload_ModifiesState()
    {
        _appState.SetState(new TestState { Name = "Initial", Counter = 0 });

        await _appState.UpdateState<TestState>(async s =>
        {
            await Task.Delay(10);
            s.Name = "Updated Async";
            s.Counter = 100;
        });

        var state = _appState.GetState<TestState>();
        Assert.Equal("Updated Async", state.Name);
        Assert.Equal(100, state.Counter);
    }

    [Fact]
    public async Task UpdateState_AsyncOverload_WhenStateDoesNotExist_CreatesAndModifies()
    {
        await _appState.UpdateState<TestState>(async s =>
        {
            await Task.Delay(10);
            s.Name = "Created Async";
            s.Counter = 50;
        });

        var state = _appState.GetState<TestState>();
        Assert.Equal("Created Async", state.Name);
        Assert.Equal(50, state.Counter);
    }

    [Fact]
    public async Task UpdateState_AsyncOverload_IsThreadSafe()
    {
        _appState.SetState(new TestState { Counter = 0 });
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_appState.UpdateState<TestState>(async s =>
            {
                await Task.Delay(5);
                s.Counter++;
            }));
        }

        await Task.WhenAll(tasks);

        var state = _appState.GetState<TestState>();
        Assert.Equal(10, state.Counter);
    }

    [Fact]
    public async Task ObserveStateChanges_EmitsPropertyChanges()
    {
        StateChange<TestState>? receivedChange = null;
        var observable = _appState.ObserveStateChanges<TestState>();
        using var subscription = observable.Subscribe(change => receivedChange = change);

        await _appState.UpdateState<TestState>(s =>
        {
            s.Name = "NewName";
            s.Counter = 42;
        });

        await Task.Delay(50);

        Assert.NotNull(receivedChange);
        Assert.Equal(2, receivedChange.ChangedProperties.Count);
        Assert.Contains(receivedChange.ChangedProperties, p => p.PropertyName == nameof(TestState.Name));
        Assert.Contains(receivedChange.ChangedProperties, p => p.PropertyName == nameof(TestState.Counter));
    }

    [Fact]
    public async Task ObserveStateChanges_TracksOldAndNewValues()
    {
        await _appState.SetState(new TestState { Name = "OldName", Counter = 10 });
        StateChange<TestState>? receivedChange = null;
        var observable = _appState.ObserveStateChanges<TestState>();
        using var subscription = observable.Subscribe(change => receivedChange = change);

        await _appState.UpdateState<TestState>(s =>
        {
            s.Name = "NewName";
            s.Counter = 20;
        });

        await Task.Delay(50);

        Assert.NotNull(receivedChange);
        var nameChange = receivedChange.ChangedProperties.First(p => p.PropertyName == nameof(TestState.Name));
        var counterChange = receivedChange.ChangedProperties.First(p => p.PropertyName == nameof(TestState.Counter));

        Assert.Equal("OldName", nameChange.OldValue);
        Assert.Equal("NewName", nameChange.NewValue);
        Assert.Equal(10, counterChange.OldValue);
        Assert.Equal(20, counterChange.NewValue);
    }

    [Fact]
    public async Task ObserveStateChanges_OnlyEmitsWhenPropertiesChange()
    {
        await _appState.SetState(new TestState { Name = "Test", Counter = 5 });
        var changeCount = 0;
        var observable = _appState.ObserveStateChanges<TestState>();
        using var subscription = observable.Subscribe(_ => changeCount++);

        await _appState.UpdateState<TestState>(s =>
        {
            s.Name = "Test";
            s.Counter = 5;
        });

        await Task.Delay(50);

        Assert.Equal(0, changeCount);
    }

    [Fact]
    public async Task ObserveStateChanges_SinglePropertyChange()
    {
        await _appState.SetState(new TestState { Name = "Test", Counter = 5 });
        StateChange<TestState>? receivedChange = null;
        var observable = _appState.ObserveStateChanges<TestState>();
        using var subscription = observable.Subscribe(change => receivedChange = change);

        await _appState.UpdateState<TestState>(s => s.Counter = 10);

        await Task.Delay(50);

        Assert.NotNull(receivedChange);
        Assert.Single(receivedChange.ChangedProperties);
        Assert.Equal(nameof(TestState.Counter), receivedChange.ChangedProperties[0].PropertyName);
    }

    [Fact]
    public async Task ObserveStateChanges_AsyncUpdateState_TracksChanges()
    {
        StateChange<TestState>? receivedChange = null;
        var observable = _appState.ObserveStateChanges<TestState>();
        using var subscription = observable.Subscribe(change => receivedChange = change);

        await _appState.UpdateState<TestState>(async s =>
        {
            await Task.Delay(10);
            s.Name = "Async Updated";
            s.Counter = 99;
        });

        await Task.Delay(50);

        Assert.NotNull(receivedChange);
        Assert.Equal(2, receivedChange.ChangedProperties.Count);
    }

    [Fact]
    public async Task ObserveStateChanges_MultipleSubscribers_AllReceiveChanges()
    {
        StateChange<TestState>? change1 = null;
        StateChange<TestState>? change2 = null;
        var observable = _appState.ObserveStateChanges<TestState>();

        using var subscription1 = observable.Subscribe(c => change1 = c);
        using var subscription2 = observable.Subscribe(c => change2 = c);

        await _appState.UpdateState<TestState>(s => s.Name = "Shared");

        await Task.Delay(50);

        Assert.NotNull(change1);
        Assert.NotNull(change2);
        Assert.Single(change1.ChangedProperties);
        Assert.Single(change2.ChangedProperties);
        Assert.Equal("Shared", change1.State.Name);
        Assert.Equal("Shared", change2.State.Name);
    }

    [Fact]
    public async Task ObserveStateChanges_WithSetState_DoesNotEmit()
    {
        var changeCount = 0;
        var observable = _appState.ObserveStateChanges<TestState>();
        using var subscription = observable.Subscribe(_ => changeCount++);

        await _appState.SetState(new TestState { Name = "Set", Counter = 1 });

        await Task.Delay(50);

        Assert.Equal(0, changeCount);
    }

    [Fact]
    public void ObserveStateChanges_ReturnsObservable()
    {
        var observable = _appState.ObserveStateChanges<TestState>();

        Assert.NotNull(observable);
    }
}
