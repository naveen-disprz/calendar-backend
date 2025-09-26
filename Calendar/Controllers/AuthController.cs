using Calendar.Business;
using Calendar.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthBL _authBL;
        private readonly ILogger<AuthController> _logger;
        private readonly ICookieHelper _cookieHelper;


        public AuthController(
            IAuthBL authBL,
            ILogger<AuthController> logger,
            ICookieHelper cookieHelper = null
        )

        {
            _authBL = authBL;
            _logger = logger;
            _cookieHelper = cookieHelper;

        }

        [HttpPost("signup")]
        public async Task<ActionResult<AuthResponseDto>> SignupAsync([FromBody] SignupRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ErrorResponseDto("Validation failed", errors));
                }

                var result = await _authBL.SignupAsync(request);

                // Don't send token in response body
                result.Token = null; // Or remove from DTO entirely

                _logger.LogInformation("User signup successful: {Email}", request.Email);

                return StatusCode(StatusCodes.Status201Created);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Signup failed - user exists: {Email}, Error: {Message}",
                    request.Email, ex.Message);
                return Conflict(new ErrorResponseDto(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Signup failed: {Email}", request.Email);

                return StatusCode(500, new ErrorResponseDto("An error occurred during registration"));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> LoginAsync([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ErrorResponseDto("Validation failed", errors));
                }

                var result = await _authBL.LoginAsync(request);

                // Set JWT in httpOnly cookie
                _cookieHelper?.SetAuthenticationCookie(result.Token!, result.ExpiresAt);
                
                _logger.LogInformation("User login successful: {Email}", request.Email);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed - invalid credentials: {Email}, Error: {Message}",
                    request.Email, ex.Message);
                return Unauthorized(new ErrorResponseDto("Invalid email or password"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed: {Email}", request.Email);
                return StatusCode(500, new ErrorResponseDto("An error occurred during login"));
                // return StatusCode(500, new ErrorResponseDto(ex.Message));
            }
        }
        
        [HttpPost("logout")]
public IActionResult LogoutAsync()
{
    try
    {
        // Clear the auth cookie by setting it with an expired date
        _cookieHelper?.ClearAuthenticationCookie();

        _logger.LogInformation("User logged out successfully");

        return Ok(new { message = "Logout successful" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Logout failed");
        return StatusCode(500, new ErrorResponseDto("An error occurred during logout"));
    }
}
    }
}