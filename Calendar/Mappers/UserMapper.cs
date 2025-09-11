using Calendar.Models;
using Calendar.DTOs.User;

namespace Calendar.Mappers;

public static class UserMapper
{
    public static User ToEntity(UserSignupRequestDto requestDto)
    {
        return new User
        {
            UserId = Guid.NewGuid(),
            UserName = requestDto.UserName,
            Email = requestDto.Email,
            // PasswordHash will be set in the service, not here
        };
    }

    public static UserSignupResponseDto ToSignupResponseDto(User user)
    {
        return new UserSignupResponseDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Email = user.Email
        };
    }

    public static UserLoginResponseDto ToLoginResponseDto(User user, string token)
    {
        return new UserLoginResponseDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Email = user.Email,
            Token = token // Assuming you have a Token property in the User model
        };
    }
}
