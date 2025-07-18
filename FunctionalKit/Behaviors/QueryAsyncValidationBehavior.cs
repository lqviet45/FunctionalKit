using FunctionalKit.Core.Messaging;
using FunctionalKit.Core.Messaging.PipelineBehaviors;

namespace FunctionalKit.Behaviors;

/// <summary>
/// Async validation behavior for queries
/// </summary>
/// <typeparam name="TQuery">The query type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class QueryAsyncValidationBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>, IAsyncValidatable
{
    public async Task<TResponse> HandleAsync(TQuery query, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var validation = await query.ValidateAsync(cancellationToken).ConfigureAwait(false);
        if (validation.IsInvalid)
        {
            var errors = string.Join("; ", validation.Errors);
            throw new ValidationException(errors);
        }

        return await next().ConfigureAwait(false);
    }
}