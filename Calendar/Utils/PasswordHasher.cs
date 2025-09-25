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
                     ?? "YourPasswordSecretKeyThatIsAlsoVeryLongAndSecure123456";
        _logger = logger;
    }

    public virtual string HashPassword(string password)
    {
            // Generate a random salt
            var salt = GenerateSalt();
            
            // Combine password with salt and hash with HMAC
            var hash = ComputeHmac(password, salt);
            
            // Return salt + hash (base64 encoded)
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    public virtual bool VerifyPassword(string password, string hashedPassword)
    {
        {
            var parts = hashedPassword.Split(':');
            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);
            var computedHash = ComputeHmac(password, salt);
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
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
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var combined = new byte[passwordBytes.Length + salt.Length];
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, passwordBytes.Length, salt.Length);
        return hmac.ComputeHash(combined);
    }
}
