using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Calendar.Models;
using Calendar.Interfaces;
using Calendar.DTOs.User;
using BCrypt.Net;
using Calendar.Mappers;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace Calendar.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;
    
    public UserService(IUserRepository userRepository, IConfiguration config)
    {
        _config =  config;
        _userRepository = userRepository;
    }

    public async Task<UserSignupResponseDto> RegisterAsync(UserSignupRequestDto signupRequestDto)
    {
        
        // Email validation
        if (!Regex.IsMatch(signupRequestDto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new Exception("Invalid email format.");
        }

        // check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(signupRequestDto.Email);
        if (existingUser != null)
        {
            throw new Exception("User already exists with this email.");
        }
        
        // Password validation
        if (!Regex.IsMatch(signupRequestDto.Password, @"^(?=.*[A-Za-z])(?=.*\d).{8,}$"))
        {
            throw new Exception("Password must be at least 8 characters long and include at least one letter and one number.");
        }
        

        // hash password (simple placeholder here)
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(signupRequestDto.Password);

        // create entity
        var user = UserMapper.ToEntity(signupRequestDto);
        
        user.PasswordHash = hashedPassword;

        // save
        await _userRepository.AddAsync(user);

        // map back to response DTO
        return UserMapper.ToSignupResponseDto(user);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _config["Jwt:Key"];
        var jwtIssuer = _config["Jwt:Issuer"];

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.UserName),
            new Claim("userId", user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtIssuer,
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<UserLoginResponseDto> LoginAsync(UserLoginRequestDto loginRequestDto)
    {
        // Find user by email
        var user = await _userRepository.GetByEmailAsync(loginRequestDto.Email);
        if (user == null)
        {
            throw new Exception("Invalid email or password.");
        }

        // Verify password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new Exception("Invalid email or password.");
        }

        var token = GenerateJwtToken(user);

        // Map to response DTO
        return UserMapper.ToLoginResponseDto(user, token);
    }
}