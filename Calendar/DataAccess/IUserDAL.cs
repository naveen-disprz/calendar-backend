using Calendar.Models;

namespace Calendar.DataAccess;

public interface IUserDAL
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid userId);
    Task<User> CreateAsync(User user);
    Task<bool> ExistsAsync(string email);
    Task<List<User>> GetByIdsAsync(List<Guid> userIds);
    Task<List<User>> GetAllAsync(Guid? excludeUserId);
}