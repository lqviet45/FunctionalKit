using FunctionalKit.Data.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionalKit.Data;

/// <summary>
/// Repository factory implementation
/// </summary>
public class RepositoryFactory : IRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;

    public RepositoryFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IUnitOfWork<TContext> CreateUnitOfWork<TContext>() where TContext : DbContext
    {
        var context = _serviceProvider.GetRequiredService<TContext>();
        return new UnitOfWork<TContext>(context);
    }

    public IRepository<T, TKey> CreateRepository<T, TKey, TContext>() where T : class where TContext : DbContext
    {
        var context = _serviceProvider.GetRequiredService<TContext>();
        return new GenericRepository<T, TKey, TContext>(context);
    }
}