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
}
