using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Users;

public class UpdateUserStatusDto
{
    [Required]
    public bool? IsActive { get; set; }
}
