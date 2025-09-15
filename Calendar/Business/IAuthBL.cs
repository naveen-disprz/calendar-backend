using Calendar.DTOs;

namespace Calendar.Business;

public interface IAuthBL
{
    Task<AuthResponseDto> SignupAsync(SignupRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
}