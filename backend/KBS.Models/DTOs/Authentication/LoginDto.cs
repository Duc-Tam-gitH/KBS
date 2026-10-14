namespace KBS.Models.DTOs.Authentication;

public class LoginDto
{
    public string LoginIdentifier { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}