using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Data;
using DAL.Repositories.Repository_Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Repository_Classes
{
    public class GenriceRepository<T> : IGenriceRepository<T> where T : class
    {
        protected readonly StoreDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenriceRepository(StoreDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Delete(T entity) => _dbSet.Remove(entity);
    }
}
