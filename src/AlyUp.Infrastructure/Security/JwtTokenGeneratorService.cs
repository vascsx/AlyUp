using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AlyUp.Application.Interfaces;
using AlyUp.Application.Security;
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
        var lifetimeInMinutes = _configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;
        if (lifetimeInMinutes <= 0)
        {
            throw new InvalidOperationException("JWT access token lifetime must be greater than zero.");
        }

        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId, user.Id.ToString()),
            new(AppClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.SalonId.HasValue)
        {
            claims.Add(new Claim(AppClaimTypes.SalonId, user.SalonId.Value.ToString()));
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
            expires: DateTime.UtcNow.AddMinutes(lifetimeInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
