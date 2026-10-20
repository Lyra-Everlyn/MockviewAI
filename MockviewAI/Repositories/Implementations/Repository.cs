using Microsoft.EntityFrameworkCore;
using MockviewAI.Data;
using MockviewAI.Repositories.Interfaces;
using System.Linq.Expressions;

namespace MockviewAI.Repositories.Implementations
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _dbSet.AsNoTracking()
                               .Skip((pageNumber - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking()
                               .Where(predicate)
                               .ToListAsync();
        }

        public async Task<T?> GetByIdAsNoTrackingAsync(int id)
        {
            var entityType = _context.Model.FindEntityType(typeof(T));
            var keyProperty = entityType?.FindPrimaryKey()?.Properties.FirstOrDefault();

            if (keyProperty == null) return null;

            return await _dbSet.AsNoTracking()
                               .FirstOrDefaultAsync(e => EF.Property<int>(e, keyProperty.Name) == id);
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<IEnumerable<T>> GetListAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? take = null,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.AsNoTracking().Where(predicate);

            // Kéo thêm dữ liệu bảng phụ
            foreach (var include in includes) { query = query.Include(include); }

            // Sắp xếp
            if (orderBy != null) { query = orderBy(query); }

            // Cắt bớt (Top N)
            if (take.HasValue) { query = query.Take(take.Value); }

            return await query.ToListAsync();

        }



        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
