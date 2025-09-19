using System.Security.Cryptography;
using System.Text;

namespace Calendar.Utils;

public class PasswordHasher
{
    private readonly string _secretKey;
    private readonly ILogger<PasswordHasher> _logger;

    public PasswordHasher(IConfiguration configuration, ILogger<PasswordHasher> logger)
    {
        _secretKey = configuration["PASSWORD_SECRET_KEY"] 
                     ?? throw new InvalidOperationException("PASSWORD_SECRET_KEY is not configured");
        _logger = logger;
    }

    public string HashPassword(string password)
    {
        try
        {
            // Generate a random salt
            var salt = GenerateSalt();
            
            // Combine password with salt and hash with HMAC
            var hash = ComputeHmac(password, salt);
            
            // Return salt + hash (base64 encoded)
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hashing password");
            throw;
        }
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            // Split the stored hash to get salt and hash
            var parts = hashedPassword.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);

            // Compute hash with the same salt
            var computedHash = ComputeHmac(password, salt);

            // Compare hashes
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }

    private byte[] GenerateSalt()
    {
        var salt = new byte[32]; // 256 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    private byte[] ComputeHmac(string password, byte[] salt)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        
        // Combine password and salt
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var combined = new byte[passwordBytes.Length + salt.Length];
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, passwordBytes.Length, salt.Length);
        
        // Compute HMAC
        return hmac.ComputeHash(combined);
    }
}
