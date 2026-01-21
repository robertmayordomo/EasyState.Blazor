using Microsoft.Extensions.DependencyInjection;

namespace EasyState.Blazor.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStateManagement_RegistersAppStateAsScoped()
    {
        var services = new ServiceCollection();

        services.AddStateManagement();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAppState));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(AppState), descriptor.ImplementationType);
    }

    [Fact]
    public void AddStateManagement_RegistersEventAggregatorAsScoped()
    {
        var services = new ServiceCollection();

        services.AddStateManagement();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEventAggregator));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(EventAggregator), descriptor.ImplementationType);
    }

    [Fact]
    public void AddStateManagement_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddStateManagement();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddStateManagement_ServicesCanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddStateManagement();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var appState = scope.ServiceProvider.GetService<IAppState>();
        var eventAggregator = scope.ServiceProvider.GetService<IEventAggregator>();

        Assert.NotNull(appState);
        Assert.NotNull(eventAggregator);
        Assert.IsType<AppState>(appState);
        Assert.IsType<EventAggregator>(eventAggregator);
    }

    [Fact]
    public void AddStateManagement_ScopedInstances_AreSameWithinScope()
    {
        var services = new ServiceCollection();
        services.AddStateManagement();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var appState1 = scope.ServiceProvider.GetService<IAppState>();
        var appState2 = scope.ServiceProvider.GetService<IAppState>();

        Assert.Same(appState1, appState2);
    }

    [Fact]
    public void AddStateManagement_ScopedInstances_AreDifferentAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddStateManagement();
        using var provider = services.BuildServiceProvider();

        IAppState? appState1, appState2;
        using (var scope1 = provider.CreateScope())
        {
            appState1 = scope1.ServiceProvider.GetService<IAppState>();
        }
        using (var scope2 = provider.CreateScope())
        {
            appState2 = scope2.ServiceProvider.GetService<IAppState>();
        }

        Assert.NotSame(appState1, appState2);
    }

    [Fact]
    public void AddGlobalStateManagement_RegistersAppStateAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddGlobalStateManagement();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAppState));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(AppState), descriptor.ImplementationType);
    }

    [Fact]
    public void AddGlobalStateManagement_RegistersEventAggregatorAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddGlobalStateManagement();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEventAggregator));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(EventAggregator), descriptor.ImplementationType);
    }

    [Fact]
    public void AddGlobalStateManagement_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddGlobalStateManagement();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddGlobalStateManagement_ServicesCanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddGlobalStateManagement();
        using var provider = services.BuildServiceProvider();

        var appState = provider.GetService<IAppState>();
        var eventAggregator = provider.GetService<IEventAggregator>();

        Assert.NotNull(appState);
        Assert.NotNull(eventAggregator);
        Assert.IsType<AppState>(appState);
        Assert.IsType<EventAggregator>(eventAggregator);
    }

    [Fact]
    public void AddGlobalStateManagement_SingletonInstances_AreSameAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddGlobalStateManagement();
        using var provider = services.BuildServiceProvider();

        IAppState? appState1, appState2;
        using (var scope1 = provider.CreateScope())
        {
            appState1 = scope1.ServiceProvider.GetService<IAppState>();
        }
        using (var scope2 = provider.CreateScope())
        {
            appState2 = scope2.ServiceProvider.GetService<IAppState>();
        }

        Assert.Same(appState1, appState2);
    }

    [Fact]
    public void AddGlobalStateManagement_SingletonState_SharedAcrossRequests()
    {
        var services = new ServiceCollection();
        services.AddGlobalStateManagement();
        using var provider = services.BuildServiceProvider();

        using (var scope1 = provider.CreateScope())
        {
            var appState = scope1.ServiceProvider.GetRequiredService<IAppState>();
            appState.SetState(new TestSharedState { Value = 42 });
        }

        int? retrievedValue;
        using (var scope2 = provider.CreateScope())
        {
            var appState = scope2.ServiceProvider.GetRequiredService<IAppState>();
            retrievedValue = appState.GetState<TestSharedState>().Value;
        }

        Assert.Equal(42, retrievedValue);
    }

    private class TestSharedState
    {
        public int Value { get; set; }
    }

    [Fact]
    public void AddStateManagement_CanBeChainedWithOtherMethods()
    {
        var services = new ServiceCollection();

        services
            .AddStateManagement()
            .AddLogging();
    }

    [Fact]
    public void AddGlobalStateManagement_CanBeChainedWithOtherMethods()
    {
        var services = new ServiceCollection();

        services
            .AddGlobalStateManagement()
            .AddLogging();
    }
}
