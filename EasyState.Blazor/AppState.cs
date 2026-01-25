using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Reflection;

namespace EasyState.Blazor;

public class AppState : IAppState, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _states = new();
    private readonly ConcurrentDictionary<Type, object> _subjects = new();
    private readonly ConcurrentDictionary<Type, object> _changeSubjects = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public T GetState<T>() where T : class, new()
    {
        return (T)_states.GetOrAdd(typeof(T), _ => new T());
    }

    public Task SetState<T>(T state) where T : class
    {
        _states.AddOrUpdate(typeof(T), state, (_, _) => state);
        NotifyStateChanged(state);
        return Task.CompletedTask;
    }

    public async Task UpdateState<T>(Action<T> updateAction) where T : class, new()
    {
        await _lock.WaitAsync();
        try
        {
            var state = GetState<T>();
            var snapshot = CreateSnapshot(state);

            updateAction(state);

            var changes = DetectChanges(snapshot, state);
            NotifyStateChanged(state);
            NotifyPropertyChanges(state, changes);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateState<T>(Func<T, Task> updateAction) where T : class, new()
    {
        await _lock.WaitAsync();
        try
        {
            var state = GetState<T>();
            var snapshot = CreateSnapshot(state);

            await updateAction(state);

            var changes = DetectChanges(snapshot, state);
            NotifyStateChanged(state);
            NotifyPropertyChanges(state, changes);
        }
        finally
        {
            _lock.Release();
        }
    }

    public IObservable<T> ObserveState<T>() where T : class, new()
    {
        var subject = (BehaviorSubject<T>)_subjects.GetOrAdd(
            typeof(T),
            _ => new BehaviorSubject<T>(GetState<T>())
        );
        return subject;
    }

    public IObservable<StateChange<T>> ObserveStateChanges<T>() where T : class, new()
    {
        var subject = (Subject<StateChange<T>>)_changeSubjects.GetOrAdd(
            typeof(T),
            _ => new Subject<StateChange<T>>()
        );
        return subject;
    }

    private void NotifyStateChanged<T>(T state) where T : class
    {
        if (_subjects.TryGetValue(typeof(T), out var subjectObj))
        {
            var subject = (BehaviorSubject<T>)subjectObj;
            subject.OnNext(state);
        }
    }

    private void NotifyPropertyChanges<T>(T state, List<PropertyChange> changes) where T : class
    {
        if (changes.Count > 0 && _changeSubjects.TryGetValue(typeof(T), out var subjectObj))
        {
            var subject = (Subject<StateChange<T>>)subjectObj;
            var stateChange = new StateChange<T>(state, changes);
            subject.OnNext(stateChange);
        }
    }

    private Dictionary<string, object?> CreateSnapshot<T>(T state) where T : class
    {
        var snapshot = new Dictionary<string, object?>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            snapshot[property.Name] = property.GetValue(state);
        }

        return snapshot;
    }

    private List<PropertyChange> DetectChanges<T>(Dictionary<string, object?> snapshot, T state) where T : class
    {
        var changes = new List<PropertyChange>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            var oldValue = snapshot[property.Name];
            var newValue = property.GetValue(state);

            if (!Equals(oldValue, newValue))
            {
                changes.Add(new PropertyChange(property.Name, oldValue, newValue));
            }
        }

        return changes;
    }

    public void Dispose()
    {
        foreach (var subject in _subjects.Values)
        {
            if (subject is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        foreach (var subject in _changeSubjects.Values)
        {
            if (subject is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _lock.Dispose();
    }
}