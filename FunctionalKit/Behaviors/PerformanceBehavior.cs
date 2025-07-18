using System.Diagnostics;
using Microsoft.Extensions.Logging;
using FunctionalKit.Core.Messaging;
using FunctionalKit.Core.Messaging.PipelineBehaviors;

namespace FunctionalKit.Behaviors;

/// <summary>
/// Performance monitoring behavior for queries
/// </summary>
public class QueryPerformanceBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly ILogger<QueryPerformanceBehavior<TQuery, TResponse>> _logger;
    private readonly long _slowQueryThresholdMs;

    public QueryPerformanceBehavior(ILogger<QueryPerformanceBehavior<TQuery, TResponse>> logger, long slowQueryThresholdMs = 500)
    {
        _logger = logger;
        _slowQueryThresholdMs = slowQueryThresholdMs;
    }

    public async Task<TResponse> HandleAsync(TQuery query, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await next();
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > _slowQueryThresholdMs)
            {
                _logger.LogWarning("Slow query {QueryType} took {ElapsedMs}ms", 
                    typeof(TQuery).Name, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            _logger.LogError("Query {QueryType} failed after {ElapsedMs}ms", 
                typeof(TQuery).Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}