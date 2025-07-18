using Microsoft.Extensions.DependencyInjection;

namespace FunctionalKit.Core.Messaging;

/// <summary>
/// Enhanced messenger with pipeline behavior support
/// </summary>
public class PipelineMessenger : IMessenger
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Messenger _innerMessenger;

    public PipelineMessenger(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _innerMessenger = new Messenger(serviceProvider);
    }

    public async Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var behaviors = _serviceProvider.GetServices<Core.Messaging.PipelineBehaviors.IQueryPipelineBehavior<IQuery<TResponse>, TResponse>>()
            .Reverse()
            .ToList();

        async Task<TResponse> Handler() => await _innerMessenger.QueryAsync(query, cancellationToken).ConfigureAwait(false);

        var pipeline = behaviors.Aggregate(
            (Func<Task<TResponse>>)Handler,
            (next, behavior) => () => behavior.HandleAsync(query, next, cancellationToken));

        return await pipeline().ConfigureAwait(false);
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var behaviors = _serviceProvider.GetServices<Core.Messaging.PipelineBehaviors.ICommandPipelineBehavior<ICommand>>()
            .Reverse()
            .ToList();

        async Task Handler() => await _innerMessenger.SendAsync(command, cancellationToken).ConfigureAwait(false);

        var pipeline = behaviors.Aggregate(
            (Func<Task>)Handler,
            (next, behavior) => () => behavior.HandleAsync(command, next, cancellationToken));

        await pipeline().ConfigureAwait(false);
    }

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        var behaviors = _serviceProvider.GetServices<Core.Messaging.PipelineBehaviors.ICommandPipelineBehavior<ICommand<TResponse>, TResponse>>()
            .Reverse()
            .ToList();

        async Task<TResponse> Handler() => await _innerMessenger.SendAsync(command, cancellationToken).ConfigureAwait(false);

        var pipeline = behaviors.Aggregate(
            (Func<Task<TResponse>>)Handler,
            (next, behavior) => () => behavior.HandleAsync(command, next, cancellationToken));

        return await pipeline().ConfigureAwait(false);
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        await _innerMessenger.PublishAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}