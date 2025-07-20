using System.Linq.Expressions;
using FunctionalKit.Core;

namespace FunctionalKit.Data.Abstraction;

/// <summary>
/// Simple repository interface
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
/// <typeparam name="TKey">Key type</typeparam>
public interface IRepository<T, in TKey> where T : class
{
    // Read operations - basic
    Task<Optional<T>> GetByIdAsync(TKey id);
    Task<Optional<T>> GetFirstAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate);
    
    // Read operations - with string includes
    Task<Optional<T>> GetByIdAsync(TKey id, params string[] includes);
    Task<Optional<T>> GetFirstAsync(Expression<Func<T, bool>> predicate, params string[] includes);
    Task<IEnumerable<T>> GetAllAsync(params string[] includes);
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, params string[] includes);
    
    // Read operations - with expression includes
    Task<Optional<T>> GetByIdAsync(TKey id, params Expression<Func<T, object>>[] includes);
    Task<Optional<T>> GetFirstAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    
    // Aggregation operations
    Task<bool> ExistsAsync(TKey id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    
    // Flexible querying
    IQueryable<T> Query();
    IQueryable<T> Query(Expression<Func<T, bool>> predicate);
    
    // Write operations
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
    void Delete(TKey id);
    void AddRange(IEnumerable<T> entities);
    void UpdateRange(IEnumerable<T> entities);
    void DeleteRange(IEnumerable<T> entities);
}