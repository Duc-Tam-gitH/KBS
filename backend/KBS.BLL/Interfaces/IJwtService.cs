using KBS.Models.Entities;

namespace KBS.BLL.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, string roleName);
}