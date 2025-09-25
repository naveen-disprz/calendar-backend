using Calendar.Models;

namespace Calendar.Utils;

public interface IJwtHelper
{
    string GenerateToken(User user);
    DateTime GetTokenExpiry();
}