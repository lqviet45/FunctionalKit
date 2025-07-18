using FunctionalKit.Core.Messaging;
using FunctionalKit.Core.Messaging.PipelineBehaviors;

namespace FunctionalKit.Behaviors;

/// <summary>
/// Retry behavior for queries
/// </summary>
public class QueryRetryBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;

    public QueryRetryBehavior(int maxRetries = 3, TimeSpan? delay = null)
    {
        _maxRetries = maxRetries;
        _delay = delay ?? TimeSpan.FromSeconds(1);
    }

    public async Task<TResponse> HandleAsync(TQuery query, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await next();
            }
            catch (Exception) when (attempt < _maxRetries)
            {
                var delayMs = (int)(_delay.TotalMilliseconds * (attempt + 1));
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        return await next(); // Final attempt
    }
}