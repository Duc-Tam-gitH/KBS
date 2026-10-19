using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Authentication;

public class LoginDto
{
    [Required]
    public string LoginIdentifier { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
