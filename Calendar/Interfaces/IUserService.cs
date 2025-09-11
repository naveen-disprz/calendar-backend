using Calendar.DTOs.User;
namespace Calendar.Interfaces;

public interface IUserService
{
    Task<UserResponseDto> RegisterAsync(UserSignupDto signupDto);
    Task<UserResponseDto> LoginAsync(UserLoginDto loginDto);
}
