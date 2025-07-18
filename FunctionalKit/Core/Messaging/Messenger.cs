using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionalKit.Core.Messaging;

/// <summary>
/// Default implementation of IMessenger
/// </summary>
public class Messenger : IMessenger
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeCache = new();

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
            throw new InvalidOperationException($"No handler found for query {queryType.Name}");

        var method = handlerType.GetMethod("HandleAsync");
        var task = (Task<TResponse>)method!.Invoke(handler, new object[] { query, cancellationToken })!;
        
        return await task;
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handlerType = GetCommandHandlerType(commandType);
        
        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
            throw new InvalidOperationException($"No handler found for command {commandType.Name}");

        var method = handlerType.GetMethod("HandleAsync");
        var task = (Task)method!.Invoke(handler, new object[] { command, cancellationToken })!;
        
        await task;
    }

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handlerType = GetCommandHandlerType(commandType, typeof(TResponse));
        
        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
            throw new InvalidOperationException($"No handler found for command {commandType.Name}");

        var method = handlerType.GetMethod("HandleAsync");
        var task = (Task<TResponse>)method!.Invoke(handler, new object[] { command, cancellationToken })!;
        
        return await task;
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));

        var notificationType = typeof(TNotification);
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        
        var handlers = _serviceProvider.GetServices(handlerType);
        
        var tasks = handlers.Select(async handler =>
        {
            var method = handlerType.GetMethod("HandleAsync");
            var task = (Task)method!.Invoke(handler, new object[] { notification, cancellationToken })!;
            await task;
        });

        await Task.WhenAll(tasks);
    }

    private static Type GetQueryHandlerType(Type queryType, Type responseType)
    {
        return HandlerTypeCache.GetOrAdd(queryType, _ =>
            typeof(IQueryHandler<,>).MakeGenericType(queryType, responseType));
    }

    private static Type GetCommandHandlerType(Type commandType, Type? responseType = null)
    {
        return HandlerTypeCache.GetOrAdd(commandType, _ =>
            responseType == null 
                ? typeof(ICommandHandler<>).MakeGenericType(commandType)
                : typeof(ICommandHandler<,>).MakeGenericType(commandType, responseType));
    }
}