using AlyUp.Application.Interfaces;
using BCrypt.Net;

namespace AlyUp.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
