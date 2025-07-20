using Microsoft.EntityFrameworkCore;

namespace FunctionalKit.Data.Abstraction;

/// <summary>
/// Repository factory for managing multiple contexts
/// </summary>
public interface IRepositoryFactory
{
    IUnitOfWork<TContext> CreateUnitOfWork<TContext>() where TContext : DbContext;
    IRepository<T, TKey> CreateRepository<T, TKey, TContext>() where T : class where TContext : DbContext;
}