using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Authentication;

public class UpdateProfileDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}
