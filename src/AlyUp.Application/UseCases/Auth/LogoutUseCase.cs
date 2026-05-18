using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;

namespace AlyUp.Application.UseCases.Auth;

public class LogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(LogoutRequestDto request)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (refreshToken is null || !refreshToken.IsActive)
            return Result.Success();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var revokedAt = DateTime.UtcNow;
            refreshToken.Revoked = revokedAt;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
            if (user is not null)
            {
                user.UpdatedAt = revokedAt;
                await _userRepository.UpdateAsync(user);
            }
        });

        return Result.Success();
    }
}
