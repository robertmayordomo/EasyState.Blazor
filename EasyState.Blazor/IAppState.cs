namespace EasyState.Blazor;

public interface IAppState
{
    T GetState<T>() where T : class, new();
    Task SetState<T>(T state) where T : class;
    Task UpdateState<T>(Action<T> updateAction) where T : class, new();
    Task UpdateState<T>(Func<T, Task> updateAction) where T : class, new();
    IObservable<T> ObserveState<T>() where T : class, new();
    IObservable<StateChange<T>> ObserveStateChanges<T>() where T : class, new();
    void Dispose();
}

public class StateChange<T> where T : class
{
    public T State { get; }
    public IReadOnlyList<PropertyChange> ChangedProperties { get; }

    public StateChange(T state, IReadOnlyList<PropertyChange> changedProperties)
    {
        State = state;
        ChangedProperties = changedProperties;
    }
}

public class PropertyChange
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public PropertyChange(string propertyName, object? oldValue, object? newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}