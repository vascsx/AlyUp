using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;

namespace AlyUp.Application.UseCases.Auth;

public class LogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        ICurrentUserService currentUserService,
        IRefreshTokenHasher refreshTokenHasher,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _currentUserService = currentUserService;
        _refreshTokenHasher = refreshTokenHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(LogoutRequestDto request)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
            return Result.Failure("Usuário não autenticado.");

        var refreshTokenHash = _refreshTokenHasher.Hash(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash);
        if (refreshToken is null || !refreshToken.IsActive || refreshToken.UserId != _currentUserService.UserId.Value)
            return Result.Success();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var revokedAt = DateTime.UtcNow;
            refreshToken.Revoked = revokedAt;
            await _refreshTokenRepository.UpdateAsync(refreshToken);
        });

        return Result.Success();
    }
}
