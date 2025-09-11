using Type = Calendar.Models.Type;

namespace Calendar.Repositories;

public interface ITypeRepository
{
    Task<List<Type>> GetAllAsync();
}