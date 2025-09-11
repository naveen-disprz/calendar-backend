using Calendar.Models;
using Calendar.Interfaces;
using Calendar.DTOs.User;
using BCrypt.Net;
using Calendar.Mappers;
using System.Text.RegularExpressions;

namespace Calendar.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponseDto> RegisterAsync(UserSignupDto signupDto)
    {
        
        // Email validation
        if (!Regex.IsMatch(signupDto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new Exception("Invalid email format.");
        }

        // check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(signupDto.Email);
        if (existingUser != null)
        {
            throw new Exception("User already exists with this email.");
        }
        
        // Password validation
        if (!Regex.IsMatch(signupDto.Password, @"^(?=.*[A-Za-z])(?=.*\d).{8,}$"))
        {
            throw new Exception("Password must be at least 8 characters long and include at least one letter and one number.");
        }
        

        // hash password (simple placeholder here)
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(signupDto.Password);

        // create entity
        var user = UserMapper.ToEntity(signupDto);
        
        user.PasswordHash = hashedPassword;

        // save
        await _userRepository.AddAsync(user);

        // map back to response DTO
        return UserMapper.ToDto(user);
    }

    public async Task<UserResponseDto> LoginAsync(UserLoginDto loginDto)
    {
        // Find user by email
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);
        if (user == null)
        {
            throw new Exception("Invalid email or password.");
        }

        // Verify password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new Exception("Invalid email or password.");
        }

        // Map to response DTO
        return UserMapper.ToDto(user);
    }
}