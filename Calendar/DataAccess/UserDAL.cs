using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.DataAccess;

public class UserDAL : IUserDAL
{
    private readonly AppDbContext _context;

    public UserDAL(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
    }

    public async Task<User> CreateAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
    }
    
    public async Task<User?> GetByIdAsync(Guid userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }
    
    public async Task<List<User>> GetByIdsAsync(List<Guid> userIds)
    {
        return await _context.Users
            .Where(u => userIds.Contains(u.Id) && u.IsActive)
            .ToListAsync();
    }

    public async Task<List<User>> GetAllAsync(Guid? excludeUserId)
    {
        return await _context.Users
            .Where(u => (!excludeUserId.HasValue || u.Id != excludeUserId.Value) && u.IsActive)
            .ToListAsync();
    }
}