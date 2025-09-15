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

        public AuthController(
            IAuthBL authBL,
            ILogger<AuthController> logger)
        {
            _authBL = authBL;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="request">Signup request</param>
        /// <returns>Authentication response with token</returns>
        [HttpPost("signup")]
        public async Task<ActionResult<AuthResponseDto>> Signup([FromBody] SignupRequestDto request)
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

                _logger.LogInformation("User signup successful: {Email}", request.Email);

                return Ok(result);
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

        /// <summary>
        /// Authenticate user and return token
        /// </summary>
        /// <param name="request">Login request</param>
        /// <returns>Authentication response with token</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
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
            }
        }
    }
}
