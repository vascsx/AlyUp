using System.Security.Cryptography;
using System.Text;
using AlyUp.Application.Interfaces;

namespace AlyUp.Infrastructure.Security;

public class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }
}