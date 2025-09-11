using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;
using Type = Calendar.Models.Type;

namespace Calendar.Repositories
{
    public class TypeRepository : ITypeRepository
    {
        private readonly AppDbContext _context;

        public TypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Type>> GetAllAsync()
        {
            var types = await _context.Types.ToListAsync();
            return types;
        }
    }
}