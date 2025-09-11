using Calendar.DTOs.User;
using Calendar.Interfaces;
using Calendar.Services;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;

    public AuthController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup(UserSignupRequestDto signupRequestDto)
    {
        try
        {
            var result = await _userService.RegisterAsync(signupRequestDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginRequestDto loginRequestDto)
    {
        try
        {
            var userResponse = await _userService.LoginAsync(loginRequestDto);
            return Ok(userResponse);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}