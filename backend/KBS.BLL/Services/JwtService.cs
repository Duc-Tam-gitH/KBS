using KBS.BLL.Interfaces;
using KBS.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KBS.BLL.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user, string roleName)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var issuer = jwtSettings["Issuer"]
            ?? throw new InvalidOperationException("Thiếu Jwt:Issuer.");

        var audience = jwtSettings["Audience"]
            ?? throw new InvalidOperationException("Thiếu Jwt:Audience.");

        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("Thiếu Jwt:SecretKey.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}