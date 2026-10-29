using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Users;

public class UpdateUserRoleDto
{
    [Range(1, long.MaxValue)]
    public long RoleId { get; set; }
}
