using System.Reactive.Linq;
using Microsoft.AspNetCore.Components;

namespace EasyState.Blazor;

public abstract class StateComponentBase : ComponentBase, IDisposable
{
    [Inject] protected IAppState AppState { get; set; } = default!;
    [Inject] protected IEventAggregator EventAggregator { get; set; } = default!;

    private readonly List<IDisposable> _subscriptions = new();
    protected void AddDisposable(IDisposable disposable)
    {
        _subscriptions.Add(disposable);
    } 

    protected T State<T>() where T : class, new()
    {
        return AppState.GetState<T>();
    }

    protected void UpdateState<T>(Action<T> updateAction) where T : class, new()
    {
        AppState.UpdateState(updateAction);
    }

    protected void SetState<T>(T state) where T : class
    {
        AppState.SetState(state);
    }

    protected IDisposable ObserveState<T>(Action<T> onStateChanged) where T : class, new()
    {
        var subscription = AppState.ObserveState<T>()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(state =>
            {
                onStateChanged(state);
                InvokeAsync(StateHasChanged);
            });

        _subscriptions.Add(subscription);
        return subscription;
    }

    protected IDisposable ObserveState<T>(Func<T, Task> onStateChanged) where T : class, new()
    {
        var subscription = AppState.ObserveState<T>()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(state =>
            {
                InvokeAsync(async () =>
                {
                    await onStateChanged(state);
                    StateHasChanged();
                });
            });

        _subscriptions.Add(subscription);
        return subscription;
    }

    protected void PublishEvent<TEvent>(TEvent eventData) where TEvent : class
    {
        EventAggregator.Publish(eventData);
    }

    protected IDisposable SubscribeEvent<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var subscription = EventAggregator.SubscribeAction(async (TEvent e) =>
        {
            await InvokeAsync(() => handler(e));
        });

        _subscriptions.Add(subscription);
        return subscription;
    }

    protected IDisposable SubscribeEvent<TEvent>(Action<TEvent> handler, Func<TEvent, bool> predicate) where TEvent : class
    {
        var subscription = EventAggregator.SubscribeAction(
            async (TEvent e) => await InvokeAsync(() => handler(e)),
            predicate
        );

        _subscriptions.Add(subscription);
        return subscription;
    }

    public virtual void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }
        _subscriptions.Clear();
    }
}