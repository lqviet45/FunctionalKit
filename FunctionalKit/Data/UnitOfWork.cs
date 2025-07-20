using FunctionalKit.Data.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace FunctionalKit.Data;

/// <summary>
/// Unit of Work implementation for specific DbContext
/// </summary>
/// <typeparam name="TContext">DbContext type</typeparam>
public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : DbContext
{
    private readonly TContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(TContext context)
    {
        _context = context;
    }

    public TContext Context => _context;

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public IRepository<T, TKey> Repository<T, TKey>() where T : class
    {
        var type = typeof(T);
        
        if (_repositories.TryGetValue(type, out var value))
            return (IRepository<T, TKey>)value;

        var repository = new GenericRepository<T, TKey, TContext>(_context);
        _repositories[type] = repository;
        
        return repository;
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
