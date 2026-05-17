using AlyUp.Domain.Entities;

namespace AlyUp.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
