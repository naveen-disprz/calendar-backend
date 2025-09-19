using Calendar.DataAccess;
using Calendar.DTOs;
using Calendar.Models;
using Calendar.Utils;

namespace Calendar.Business;

public class AuthBL : IAuthBL
{
    private readonly IUserDAL _userDAL;
    private readonly JwtHelper _jwtHelper;
    private readonly PasswordHasher _passwordHasher;
    private readonly ILogger<AuthBL> _logger;

    public AuthBL(
        IUserDAL userDAL,
        JwtHelper jwtHelper,
        PasswordHasher passwordHasher,
        ILogger<AuthBL> logger)
    {
        _userDAL = userDAL;
        _jwtHelper = jwtHelper;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AuthResponseDto> SignupAsync(SignupRequestDto request)
    {
        try
        {
            // Check if user already exists
            if (await _userDAL.ExistsAsync(request.Email))
            {
                throw new InvalidOperationException("User with this email already exists.");
            }

            // Create user with HMAC-based password hash
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLower().Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userDAL.CreateAsync(user);

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            // Generate JWT token
            var token = _jwtHelper.GenerateToken(createdUser);
            var expiresAt = _jwtHelper.GetTokenExpiry();

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = MapToUserResponseDto(createdUser)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration: {Email}", request.Email);
            throw;
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        try
        {
            // Find user
            var user = await _userDAL.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // Validate password with HMAC verification
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            // Generate JWT token
            var token = _jwtHelper.GenerateToken(user);
            var expiresAt = _jwtHelper.GetTokenExpiry();

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = MapToUserResponseDto(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login: {Email}", request.Email);
            throw;
        }
    }

    private static UserResponseDto MapToUserResponseDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
