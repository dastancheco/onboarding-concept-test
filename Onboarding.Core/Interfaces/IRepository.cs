using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Onboarding.Core.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(params object[] keys);
        Task<List<T>> GetAllAsync();
        Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
    }
}
