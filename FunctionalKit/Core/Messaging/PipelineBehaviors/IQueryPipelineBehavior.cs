namespace FunctionalKit.Core.Messaging.PipelineBehaviors;

/// <summary>
/// Pipeline behavior for queries
/// </summary>
/// <typeparam name="TQuery">The query type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQueryPipelineBehavior<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, Func<Task<TResponse>> next, CancellationToken cancellationToken = default);
}