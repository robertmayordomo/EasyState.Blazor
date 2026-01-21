namespace EasyState.Blazor;

public interface IEventAggregator
{
    void Publish<TEvent>(TEvent eventData) where TEvent : class;
    IObservable<TEvent> Subscribe<TEvent>() where TEvent : class;
    IObservable<TEvent> Subscribe<TEvent>(Func<TEvent, bool> predicate) where TEvent : class;
    IDisposable SubscribeAction<TEvent>(Action<TEvent> handler) where TEvent : class;
    IDisposable SubscribeAction<TEvent>(Action<TEvent> handler, Func<TEvent, bool> predicate) where TEvent : class;
}