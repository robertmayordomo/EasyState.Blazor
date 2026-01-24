namespace EasyState.Blazor;

public interface IAppState
{
    T GetState<T>() where T : class, new();
    Task SetState<T>(T state) where T : class;
    Task UpdateState<T>(Action<T> updateAction) where T : class, new();
    Task UpdateState<T>(Func<T, Task> updateAction) where T : class, new();
    IObservable<T> ObserveState<T>() where T : class, new();
    void Dispose();
}