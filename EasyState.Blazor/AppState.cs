using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace EasyState.Blazor;

public class AppState : IAppState, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _states = new();
    private readonly ConcurrentDictionary<Type, object> _subjects = new();
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
            updateAction(state);
            NotifyStateChanged(state);
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

    private void NotifyStateChanged<T>(T state) where T : class
    {
        if (_subjects.TryGetValue(typeof(T), out var subjectObj))
        {
            var subject = (BehaviorSubject<T>)subjectObj;
            subject.OnNext(state);
        }
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
        _lock.Dispose();
    }
}