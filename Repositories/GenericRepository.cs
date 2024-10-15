using APICatalogo.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace APICatalogo.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class
{

    protected AppDbContext _context;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().AsNoTracking().ToListAsync();
    }
    public async Task<T> GetAsync(Expression<Func<T, bool>> predicate)
    {
        var entity = await _context.Set<T>().FirstOrDefaultAsync(predicate);
        return entity;
    }
    public T Create(T entity)
    {
        _context.Set<T>().Add(entity);
        _context.SaveChanges();
        return entity;
    }
    public T Update(T entity)
    {
        _context.Set<T>().Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
        _context.SaveChanges();
        return entity;
    }
    public T Delete(T entity)
    {
        _context.Set<T>().Attach(entity);
        _context.Set<T>().Remove(entity);
        _context.SaveChanges();
        return entity;
    }
}
