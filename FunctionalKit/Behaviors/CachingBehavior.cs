using Microsoft.Extensions.Caching.Memory;
using FunctionalKit.Core.Messaging;
using FunctionalKit.Core.Messaging.PipelineBehaviors;

namespace FunctionalKit.Behaviors;

/// <summary>
/// Interface for cacheable queries
/// </summary>
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan CacheDuration { get; }
}

/// <summary>
/// Caching behavior for queries
/// </summary>
public class QueryCachingBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>, ICacheable
{
    private readonly IMemoryCache _cache;

    public QueryCachingBehavior(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> HandleAsync(TQuery query, Func<Task<TResponse>> next, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(query.CacheKey, out TResponse? cachedResult))
        {
            return cachedResult!;
        }

        var result = await next();
        _cache.Set(query.CacheKey, result, query.CacheDuration);
        return result;
    }
}