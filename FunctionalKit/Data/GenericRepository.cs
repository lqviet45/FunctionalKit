using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace FunctionalKit.Data;

/// <summary>
/// Generic repository that works with any DbContext for entities with Id property
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
/// <typeparam name="TKey">Key type</typeparam>
/// <typeparam name="TContext">DbContext type</typeparam>
public class GenericRepository<T, TKey, TContext> : RepositoryBase<T, TKey, TContext> 
    where T : class
    where TContext : DbContext
{
    public GenericRepository(TContext context) : base(context) { }

    protected override TKey GetEntityId(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException($"Entity {typeof(T).Name} must have an 'Id' property");
        
        return (TKey)idProperty.GetValue(entity)!;
    }

    protected override Expression<Func<T, bool>> GetByIdExpression(TKey id)
    {
        var parameter = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(parameter, "Id");
        var constant = Expression.Constant(id);
        var equal = Expression.Equal(property, constant);
        
        return Expression.Lambda<Func<T, bool>>(equal, parameter);
    }
}