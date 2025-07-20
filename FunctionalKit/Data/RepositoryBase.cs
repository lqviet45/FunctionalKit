using System.Linq.Expressions;
using FunctionalKit.Core;
using FunctionalKit.Data.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace FunctionalKit.Data;

/// <summary>
/// Base repository implementation that works with any DbContext
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
/// <typeparam name="TKey">Key type</typeparam>
/// <typeparam name="TContext">DbContext type</typeparam>
public abstract class RepositoryBase<T, TKey, TContext> : IRepository<T, TKey> 
    where T : class
    where TContext : DbContext
{
    protected readonly TContext Context;
    protected readonly DbSet<T> DbSet;

    protected RepositoryBase(TContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    // Abstract methods for ID handling
    protected abstract TKey GetEntityId(T entity);
    protected abstract Expression<Func<T, bool>> GetByIdExpression(TKey id);

    #region Read Operations

    public virtual async Task<Optional<T>> GetByIdAsync(TKey id)
    {
        var entity = await DbSet.FirstOrDefaultAsync(GetByIdExpression(id));
        return Optional<T>.OfNullable(entity);
    }

    public virtual async Task<Optional<T>> GetByIdAsync(TKey id, params string[] includes)
    {
        var query = ApplyIncludes(DbSet.AsQueryable(), includes);
        var entity = await query.FirstOrDefaultAsync(GetByIdExpression(id));
        return Optional<T>.OfNullable(entity);
    }

    public virtual async Task<Optional<T>> GetByIdAsync(TKey id, params Expression<Func<T, object>>[] includes)
    {
        var query = ApplyIncludes(DbSet.AsQueryable(), includes);
        var entity = await query.FirstOrDefaultAsync(GetByIdExpression(id));
        return Optional<T>.OfNullable(entity);
    }

    public virtual async Task<Optional<T>> GetFirstAsync(Expression<Func<T, bool>> predicate)
    {
        var entity = await DbSet.FirstOrDefaultAsync(predicate);
        return Optional<T>.OfNullable(entity);
    }

    public virtual async Task<Optional<T>> GetFirstAsync(Expression<Func<T, bool>> predicate, params string[] includes)
    {
        var query = ApplyIncludes(DbSet.AsQueryable(), includes);
        var entity = await query.FirstOrDefaultAsync(predicate);
        return Optional<T>.OfNullable(entity);
    }

    public virtual async Task<Optional<T>> GetFirstAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = ApplyIncludes(DbSet.AsQueryable(), includes);
        var entity = await query.FirstOrDefaultAsync(predicate);
        return Optional<T>.OfNullable(entity);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(params string[] includes)
    {
        var query = ApplyIncludes(DbSet.AsQueryable(), includes);
        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
    {
        var query = ApplyIncludes(DbSet.AsQueryable(), includes);
        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, params string[] includes)
    {
        var query = ApplyIncludes(DbSet.AsQueryable(), includes);
        return await query.Where(predicate).ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = ApplyIncludes(DbSet.AsQueryable(), includes);
        return await query.Where(predicate).ToListAsync();
    }

    public virtual async Task<bool> ExistsAsync(TKey id)
    {
        return await DbSet.AnyAsync(GetByIdExpression(id));
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync()
    {
        return await DbSet.CountAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    #endregion

    #region Helper Methods

    protected virtual IQueryable<T> ApplyIncludes(IQueryable<T> query, params string[] includes)
    {
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }

    protected virtual IQueryable<T> ApplyIncludes(IQueryable<T> query, params Expression<Func<T, object>>[] includes)
    {
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }

    #endregion

    #region Flexible Querying

    public virtual IQueryable<T> Query()
    {
        return DbSet.AsQueryable();
    }

    public virtual IQueryable<T> Query(Expression<Func<T, bool>> predicate)
    {
        return DbSet.Where(predicate);
    }

    #endregion

    #region Write Operations

    public virtual void Add(T entity)
    {
        DbSet.Add(entity);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }

    public virtual void Delete(TKey id)
    {
        var entity = DbSet.Local.FirstOrDefault(e => GetEntityId(e).Equals(id));
        if (entity != null)
        {
            DbSet.Remove(entity);
        }
        else
        {
            // Create a stub entity for deletion if not in local cache
            entity = DbSet.Find(id);
            if (entity != null)
                DbSet.Remove(entity);
        }
    }

    public virtual void AddRange(IEnumerable<T> entities)
    {
        DbSet.AddRange(entities);
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        DbSet.UpdateRange(entities);
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
    }

    #endregion
}