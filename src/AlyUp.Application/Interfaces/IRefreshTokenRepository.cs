using AlyUp.Domain.Entities;

namespace AlyUp.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task UpdateAsync(RefreshToken refreshToken);
}
