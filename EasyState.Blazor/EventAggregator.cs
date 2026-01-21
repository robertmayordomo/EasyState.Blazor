using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EasyState.Blazor;

public class EventAggregator : IEventAggregator, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _subjects = new();

    public void Publish<TEvent>(TEvent eventData) where TEvent : class
    {
        if (eventData == null) return;

        if (_subjects.TryGetValue(typeof(TEvent), out var subjectObj))
        {
            var subject = (Subject<TEvent>)subjectObj;
            subject.OnNext(eventData);
        }
    }

    public IObservable<TEvent> Subscribe<TEvent>() where TEvent : class
    {
        var subject = (Subject<TEvent>)_subjects.GetOrAdd(
            typeof(TEvent),
            _ => new Subject<TEvent>()
        );
        return subject.AsObservable();
    }

    public IObservable<TEvent> Subscribe<TEvent>(Func<TEvent, bool> predicate) where TEvent : class
    {
        return Subscribe<TEvent>().Where(predicate);
    }

    public IDisposable SubscribeAction<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        return Subscribe<TEvent>().Subscribe(handler);
    }

    public IDisposable SubscribeAction<TEvent>(Action<TEvent> handler, Func<TEvent, bool> predicate) where TEvent : class
    {
        return Subscribe<TEvent>(predicate).Subscribe(handler);
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
        _subjects.Clear();
    }
}