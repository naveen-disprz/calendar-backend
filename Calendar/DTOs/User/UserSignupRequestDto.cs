namespace Calendar.DTOs.User;

public class UserSignupRequestDto
{
    public string UserName { get; set; }  // required
    public string Email { get; set; }     // required
    public string Password { get; set; }  // required
}
