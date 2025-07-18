namespace FunctionalKit.Core.Messaging;

/// <summary>
/// Central messaging interface (alternative to MediatR's IMediator)
/// </summary>
public interface IMessenger
{
    /// <summary>
    /// Send a query and get a response
    /// </summary>
    Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a command without return value
    /// </summary>
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a command and get a response
    /// </summary>
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publish a notification to multiple handlers
    /// </summary>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}