using AlyUp.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AlyUp.Infrastructure.Security;

public class AccessTokenLifetimeProvider : IAccessTokenLifetimeProvider
{
    private readonly IConfiguration _configuration;

    public AccessTokenLifetimeProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int GetLifetimeInMinutes()
    {
        var lifetimeInMinutes = _configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 30;
        if (lifetimeInMinutes <= 0)
        {
            throw new InvalidOperationException("JWT access token lifetime must be greater than zero.");
        }

        return lifetimeInMinutes;
    }
}
