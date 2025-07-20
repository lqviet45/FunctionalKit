using Microsoft.EntityFrameworkCore;

namespace FunctionalKit.Data.Abstraction;

/// <summary>
/// Unit of Work interface that works with any DbContext
/// </summary>
/// <typeparam name="TContext">DbContext type</typeparam>
public interface IUnitOfWork<out TContext> : IDisposable where TContext : DbContext
{
    TContext Context { get; }
    Task<int> SaveChangesAsync();
    IRepository<T, TKey> Repository<T, TKey>() where T : class;
}