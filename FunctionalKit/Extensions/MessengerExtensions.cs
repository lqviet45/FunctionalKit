using FunctionalKit.Core;
using FunctionalKit.Core.Messaging;

namespace FunctionalKit.Extensions;

/// <summary>
/// Extension methods for IMessenger
/// </summary>
public static class MessengerExtensions
{
    /// <summary>
    /// Queries and returns Optional result
    /// </summary>
    public static async Task<Optional<T>> QueryOptionalAsync<T>(
        this IMessenger messenger, 
        IQuery<Result<T>> query, 
        CancellationToken cancellationToken = default)
    {
        var result = await messenger.QueryAsync(query, cancellationToken);
        return result.ToOptional();
    }

    /// <summary>
    /// Safely executes a query and wraps in Result
    /// </summary>
    public static async Task<Result<T>> QuerySafeAsync<T>(
        this IMessenger messenger, 
        IQuery<T> query, 
        CancellationToken cancellationToken = default)
    {
        return await Functional.TryAsync(() => messenger.QueryAsync(query, cancellationToken));
    }

    /// <summary>
    /// Safely executes a command and wraps in Result
    /// </summary>
    public static async Task<Result<Unit>> SendSafeAsync(
        this IMessenger messenger, 
        ICommand command, 
        CancellationToken cancellationToken = default)
    {
        return await Functional.TryAsync(async () =>
        {
            await messenger.SendAsync(command, cancellationToken);
            return Unit.Value;
        });
    }

    /// <summary>
    /// Safely executes a command with response and wraps in Result
    /// </summary>
    public static async Task<Result<T>> SendSafeAsync<T>(
        this IMessenger messenger, 
        ICommand<T> command, 
        CancellationToken cancellationToken = default)
    {
        return await Functional.TryAsync(() => messenger.SendAsync(command, cancellationToken));
    }
}