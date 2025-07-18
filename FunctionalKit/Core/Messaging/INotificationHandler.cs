namespace FunctionalKit.Core.Messaging;

/// <summary>
/// Handler for notifications
/// </summary>
/// <typeparam name="TNotification">The notification type</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}