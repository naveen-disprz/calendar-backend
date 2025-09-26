

using System.Diagnostics.CodeAnalysis;

namespace Calendar.Utils;

[ExcludeFromCodeCoverage]
public class CookieHelper : ICookieHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CookieHelper> _logger;

    public CookieHelper(IHttpContextAccessor httpContextAccessor, ILogger<CookieHelper> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public void SetAuthenticationCookie(string token, DateTime expiresAt)
    {
        Console.WriteLine(token);
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAt
        };

        context.Response.Cookies.Append("jwt", token, cookieOptions);
    }

    public void ClearAuthenticationCookie()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("jwt");
    }
}