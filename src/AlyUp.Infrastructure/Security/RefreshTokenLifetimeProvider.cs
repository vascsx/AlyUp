using AlyUp.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AlyUp.Infrastructure.Security;

public class RefreshTokenLifetimeProvider : IRefreshTokenLifetimeProvider
{
    private readonly IConfiguration _configuration;

    public RefreshTokenLifetimeProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int GetLifetimeInDays()
    {
        var lifetimeInDays = _configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 30;
        if (lifetimeInDays <= 0)
        {
            throw new InvalidOperationException("JWT refresh token lifetime must be greater than zero.");
        }

        return lifetimeInDays;
    }
}
