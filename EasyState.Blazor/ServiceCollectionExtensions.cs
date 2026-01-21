using Microsoft.Extensions.DependencyInjection;

namespace EasyState.Blazor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStateManagement(this IServiceCollection services)
    {
        // Scoped: State is shared across all components in the same request/circuit
        // but isolated between different users/sessions
        services.AddScoped<IAppState, AppState>();

        // Scoped: Events are scoped to the same request/circuit
        services.AddScoped<IEventAggregator, EventAggregator>();

        return services;
    }

    public static IServiceCollection AddGlobalStateManagement(this IServiceCollection services)
    {
        // Singleton: State is shared across ALL users and sessions
        // Use this for truly global application state
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IEventAggregator, EventAggregator>();

        return services;
    }
}