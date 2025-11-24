using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Onboarding.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly OnboardingDbContext _context;

        public UnitOfWork(OnboardingDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
