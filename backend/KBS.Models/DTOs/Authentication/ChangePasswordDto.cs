using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Authentication;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    [RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).{8,}$", ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ và số.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
