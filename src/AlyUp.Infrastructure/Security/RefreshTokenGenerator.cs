using System.Security.Cryptography;
using AlyUp.Application.Interfaces;

namespace AlyUp.Infrastructure.Security;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
