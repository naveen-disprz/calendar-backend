using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Calendar.Models;
using Microsoft.IdentityModel.Tokens;

namespace Calendar.Utils;

public class JwtHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtHelper> _logger;

    public JwtHelper(IConfiguration configuration, ILogger<JwtHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        // Try to get JWT secret from environment variable first, then from appsettings
        var secretKey = _configuration["JWT_SECRET_KEY"] ?? _configuration["JwtSettings:SecretKey"];
        
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured");
        }

        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(GetExpiryMinutes()),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public DateTime GetTokenExpiry()
    {
        return DateTime.UtcNow.AddMinutes(GetExpiryMinutes());
    }

    private double GetExpiryMinutes()
    {
        var expiryMinutes = _configuration["JwtSettings:ExpiryMinutes"];
        return double.TryParse(expiryMinutes, out var minutes) ? minutes : 1440; // Default to 24 hours
    }
}
