using Microsoft.EntityFrameworkCore;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Onboarding.Infrastructure.Data.Repositories
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        protected readonly OnboardingDbContext _context;
        private readonly DbSet<T> _dbSet;

        public EfRepository(OnboardingDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(params object[] keys)
        {
            return await _dbSet.FindAsync(keys);
        }

        public Task UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }
    }
}
