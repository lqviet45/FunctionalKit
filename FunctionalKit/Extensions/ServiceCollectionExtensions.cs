using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FunctionalKit.Core.Messaging;
using FunctionalKit.Core.Messaging.PipelineBehaviors;
using FunctionalKit.Behaviors;
using Microsoft.Extensions.Logging;

namespace FunctionalKit.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register FunctionalKit services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Messenger and scans for handlers in the specified assemblies
    /// </summary>
    public static IServiceCollection AddFunctionalKit(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<IMessenger, Messenger>();
        
        if (assemblies.Length == 0)
            assemblies = new[] { Assembly.GetCallingAssembly() };

        RegisterHandlers(services, assemblies);
        RegisterPipelineBehaviors(services, assemblies);
        
        return services;
    }

    /// <summary>
    /// Adds logging behaviors to the pipeline
    /// </summary>
    public static IServiceCollection AddFunctionalKitLogging(this IServiceCollection services)
    {
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(QueryLoggingBehavior<,>));
        services.AddScoped(typeof(ICommandPipelineBehavior<>), typeof(CommandLoggingBehavior<>));
        return services;
    }

    /// <summary>
    /// Adds validation behaviors to the pipeline
    /// </summary>
    public static IServiceCollection AddFunctionalKitValidation(this IServiceCollection services)
    {
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(QueryValidationBehavior<,>));
        services.AddScoped(typeof(ICommandPipelineBehavior<>), typeof(CommandValidationBehavior<>));
        services.AddScoped(typeof(ICommandPipelineBehavior<,>), typeof(CommandValidationBehavior<,>));
        return services;
    }

    /// <summary>
    /// Adds caching behaviors to the pipeline
    /// </summary>
    public static IServiceCollection AddFunctionalKitCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), typeof(QueryCachingBehavior<,>));
        return services;
    }

    /// <summary>
    /// Adds performance monitoring behaviors to the pipeline
    /// </summary>
    public static IServiceCollection AddFunctionalKitPerformanceMonitoring(this IServiceCollection services, long slowQueryThresholdMs = 500)
    {
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), serviceProvider => 
        {
            var loggerType = typeof(ILogger<>).MakeGenericType(typeof(QueryPerformanceBehavior<,>));
            var logger = serviceProvider.GetRequiredService(loggerType);
            return Activator.CreateInstance(typeof(QueryPerformanceBehavior<,>), logger, slowQueryThresholdMs) ?? throw new InvalidOperationException("Failed to create QueryPerformanceBehavior instance.");
        });
        return services;
    }

    /// <summary>
    /// Adds retry behaviors to the pipeline
    /// </summary>
    public static IServiceCollection AddFunctionalKitRetry(this IServiceCollection services, int maxRetries = 3, TimeSpan? delay = null)
    {
        services.AddScoped(typeof(IQueryPipelineBehavior<,>), _ => 
            Activator.CreateInstance(typeof(QueryRetryBehavior<,>), maxRetries, delay) ?? throw new InvalidOperationException("Failed to create QueryRetryBehavior instance."));
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var handlerTypes = new[]
        {
            typeof(IQueryHandler<,>),
            typeof(ICommandHandler<>),
            typeof(ICommandHandler<,>),
            typeof(INotificationHandler<>)
        };

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
            {
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.IsGenericType)
                    {
                        var genericDefinition = interfaceType.GetGenericTypeDefinition();
                        if (handlerTypes.Contains(genericDefinition))
                        {
                            services.AddScoped(interfaceType, type);
                        }
                    }
                }
            }
        }
    }

    private static void RegisterPipelineBehaviors(IServiceCollection services, Assembly[] assemblies)
    {
        var behaviorTypes = new[]
        {
            typeof(IQueryPipelineBehavior<,>),
            typeof(ICommandPipelineBehavior<>),
            typeof(ICommandPipelineBehavior<,>)
        };

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
            {
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.IsGenericType)
                    {
                        var genericDefinition = interfaceType.GetGenericTypeDefinition();
                        if (behaviorTypes.Contains(genericDefinition))
                        {
                            services.AddScoped(interfaceType, type);
                        }
                    }
                }
            }
        }
    }
}