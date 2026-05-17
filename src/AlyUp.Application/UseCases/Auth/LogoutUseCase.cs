using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;

namespace AlyUp.Application.UseCases.Auth;

public class LogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutUseCase(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result> ExecuteAsync(LogoutRequestDto request)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (refreshToken is null || !refreshToken.IsActive)
            return Result.Success();

        refreshToken.Revoked = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(refreshToken);

        return Result.Success();
    }
}
