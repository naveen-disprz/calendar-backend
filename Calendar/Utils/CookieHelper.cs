

using System.Diagnostics.CodeAnalysis;

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
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        };

        context.Response.Cookies.Append("jwt", token, cookieOptions);
    }

    public void ClearAuthenticationCookie()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("jwt");
    }
}