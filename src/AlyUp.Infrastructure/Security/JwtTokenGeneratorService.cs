using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AlyUp.Infrastructure.Security;

public class JwtTokenGeneratorService : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGeneratorService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new("UserId", user.Id.ToString()),
            new("Name", user.Name),
            new("Email", user.Email),
            new("Role", user.Role.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.SalonId.HasValue)
        {
            claims.Add(new Claim("SalonId", user.SalonId.Value.ToString()));
        }

        var keyString = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT issuer not configured."),
            audience: _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT audience not configured."),
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
