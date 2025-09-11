using Calendar.DTOs.User;
using Calendar.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup(UserSignupDto signupDto)
    {
        try
        {
            var result = await _userService.RegisterAsync(signupDto);
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
    public async Task<IActionResult> Login(UserLoginDto loginDto)
    {
        try
        {
            var userResponse = await _userService.LoginAsync(loginDto);
            return Ok(userResponse);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
}