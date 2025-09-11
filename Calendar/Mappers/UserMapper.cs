using Calendar.Models;
using Calendar.DTOs.User;

namespace Calendar.Mappers;

public static class UserMapper
{
    public static User ToEntity(UserSignupDto dto)
    {
        return new User
        {
            UserId = Guid.NewGuid(),
            UserName = dto.UserName,
            Email = dto.Email,
            // PasswordHash will be set in the service, not here
        };
    }

    public static UserResponseDto ToDto(User user)
    {
        return new UserResponseDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Email = user.Email
        };
    }
}
