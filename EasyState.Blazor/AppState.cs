using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.Json;

namespace EasyState.Blazor;

public class AppState : IAppState, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _states = new();
    private readonly ConcurrentDictionary<Type, object> _subjects = new();
    private readonly ConcurrentDictionary<Type, object> _changeSubjects = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

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

    public async Task<StateChange<T>?> UpdateState<T>(Action<T> updateAction) where T : class, new()
    {
        await _lock.WaitAsync();
        try
        {
            var state = GetState<T>();
            var snapshot = CreateSnapshot(state);
            
            updateAction(state);
            
            var changes = DetectChanges(snapshot, state);
            NotifyStateChanged(state);
            
            if (changes.Count > 0)
            {
                var stateChange = new StateChange<T>(state, changes);
                NotifyPropertyChanges(state, changes);
                return stateChange;
            }
            
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<StateChange<T>?> UpdateState<T>(Func<T, Task> updateAction) where T : class, new()
    {
        await _lock.WaitAsync();
        try
        {
            var state = GetState<T>();
            var snapshot = CreateSnapshot(state);
            
            await updateAction(state);
            
            var changes = DetectChanges(snapshot, state);
            NotifyStateChanged(state);
            
            if (changes.Count > 0)
            {
                var stateChange = new StateChange<T>(state, changes);
                NotifyPropertyChanges(state, changes);
                return stateChange;
            }
            
            return null;
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

    private Dictionary<string, string> CreateSnapshot<T>(T state) where T : class
    {
        var snapshot = new Dictionary<string, string>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            var value = property.GetValue(state);
            snapshot[property.Name] = SerializeValue(value);
        }

        return snapshot;
    }

    private List<PropertyChange> DetectChanges<T>(Dictionary<string, string> snapshot, T state) where T : class
    {
        var changes = new List<PropertyChange>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            var oldValueSerialized = snapshot[property.Name];
            var newValue = property.GetValue(state);
            var newValueSerialized = SerializeValue(newValue);

            if (oldValueSerialized != newValueSerialized)
            {
                var oldValue = DeserializeValue(oldValueSerialized, property.PropertyType);
                changes.Add(new PropertyChange(property.Name, oldValue, newValue));
            }
        }

        return changes;
    }

    private static string SerializeValue(object? value)
    {
        if (value == null)
            return "null";

        return JsonSerializer.Serialize(value, value.GetType(), _jsonOptions);
    }

    private static object? DeserializeValue(string serialized, Type type)
    {
        if (serialized == "null")
            return null;

        return JsonSerializer.Deserialize(serialized, type, _jsonOptions);
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