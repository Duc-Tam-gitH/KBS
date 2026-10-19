using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Authentication;

public class RegisterDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).{8,}$", ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ và số.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}
