using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace FunctionalKit.Extensions;

public static class RepositoryExtensions
{
    /// <summary>
    /// Gets paged results using IQueryable
    /// </summary>
    public static async Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize) where T : class
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    /// <summary>
    /// Adds include functionality to IQueryable using strings
    /// </summary>
    public static IQueryable<T> IncludeMultiple<T>(
        this IQueryable<T> query,
        params string[] includes) where T : class
    {
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }

    /// <summary>
    /// Adds include functionality to IQueryable using expressions
    /// </summary>
    public static IQueryable<T> IncludeMultiple<T>(
        this IQueryable<T> query,
        params Expression<Func<T, object>>[] includes) where T : class
    {
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }

    /// <summary>
    /// Adds ordering to IQueryable
    /// </summary>
    public static IQueryable<T> OrderByExt<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        bool descending = false) where T : class
    {
        return descending 
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }
}