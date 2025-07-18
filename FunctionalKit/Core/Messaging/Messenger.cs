using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using FunctionalKit.Exceptions;

namespace FunctionalKit.Core.Messaging;

/// <summary>
/// Enhanced implementation of IMessenger with better error handling and performance
/// </summary>
public class Messenger : IMessenger
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<string, Type> HandlerTypeCache = new();
    private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache = new();

    public Messenger(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        var queryType = query.GetType();
        var handlerType = GetQueryHandlerType(queryType, typeof(TResponse));
        
        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
            throw new HandlerNotFoundException(queryType, handlerType);

        try
        {
            var method = GetCachedMethod(handlerType, "HandleAsync");
            var task = (Task<TResponse>)method.Invoke(handler, new object[] { query, cancellationToken })!;
            
            return await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (!(ex is HandlerNotFoundException))
        {
            throw new PipelineBehaviorException(handlerType, queryType, "Query execution failed", ex);
        }
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handlerType = GetCommandHandlerType(commandType);
        
        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
            throw new HandlerNotFoundException(commandType, handlerType);

        try
        {
            var method = GetCachedMethod(handlerType, "HandleAsync");
            var task = (Task)method.Invoke(handler, new object[] { command, cancellationToken })!;
            
            await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (!(ex is HandlerNotFoundException))
        {
            throw new PipelineBehaviorException(handlerType, commandType, "Command execution failed", ex);
        }
    }

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handlerType = GetCommandHandlerType(commandType, typeof(TResponse));
        
        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
            throw new HandlerNotFoundException(commandType, handlerType);

        try
        {
            var method = GetCachedMethod(handlerType, "HandleAsync");
            var task = (Task<TResponse>)method.Invoke(handler, new object[] { command, cancellationToken })!;
            
            return await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (!(ex is HandlerNotFoundException))
        {
            throw new PipelineBehaviorException(handlerType, commandType, "Command execution failed", ex);
        }
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));

        var notificationType = typeof(TNotification);
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        
        var handlers = _serviceProvider.GetServices(handlerType).ToList();
        
        if (!handlers.Any())
        {
            // No handlers found - this might be intentional for notifications
            return;
        }

        var method = GetCachedMethod(handlerType, "HandleAsync");
        var tasks = handlers.Select(async handler =>
        {
            try
            {
                var task = (Task)method.Invoke(handler, new object[] { notification, cancellationToken })!;
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log exception but don't fail the entire publish operation
                // In a real implementation, you might want to use ILogger here
                throw new PipelineBehaviorException(handler.GetType(), notificationType, "Notification handler failed", ex);
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static Type GetQueryHandlerType(Type queryType, Type responseType)
    {
        return HandlerTypeCache.GetOrAdd($"{queryType.FullName}_{responseType.FullName}", _ =>
            typeof(IQueryHandler<,>).MakeGenericType(queryType, responseType));
    }

    private static Type GetCommandHandlerType(Type commandType, Type? responseType = null)
    {
        var key = responseType == null ? commandType.FullName! : $"{commandType.FullName}_{responseType.FullName}";
        return HandlerTypeCache.GetOrAdd(key, _ =>
            responseType == null 
                ? typeof(ICommandHandler<>).MakeGenericType(commandType)
                : typeof(ICommandHandler<,>).MakeGenericType(commandType, responseType));
    }

    private static System.Reflection.MethodInfo GetCachedMethod(Type handlerType, string methodName)
    {
        return MethodCache.GetOrAdd($"{handlerType.FullName}_{methodName}", _ =>
        {
            var method = handlerType.GetMethod(methodName);
            if (method == null)
                throw new InvalidOperationException($"Method {methodName} not found on type {handlerType.Name}");
            return method;
        });
    }
}