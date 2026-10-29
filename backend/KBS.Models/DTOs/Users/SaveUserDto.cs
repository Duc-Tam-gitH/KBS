using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Users;

public class SaveUserDto
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;
    [StringLength(200)]
    public string? FullName { get; set; }
    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }
    [Range(1, long.MaxValue)]
    public long RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateUserDto : SaveUserDto
{
    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression("^(?=.*[A-Za-z])(?=.*\\d).{8,}$")]
    public string Password { get; set; } = string.Empty;
    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public record UserRoleDto(long Id, string Name);
