namespace KBS.Models.DTOs.Authentication;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public UserProfileDto User { get; set; } = new();
}
