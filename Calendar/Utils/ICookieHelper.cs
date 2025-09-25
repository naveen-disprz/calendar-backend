public interface ICookieHelper
{
    void SetAuthenticationCookie(string token, DateTime expiresAt);
    void ClearAuthenticationCookie();
}