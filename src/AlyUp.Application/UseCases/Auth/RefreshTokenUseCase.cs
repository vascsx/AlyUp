using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;

namespace AlyUp.Application.UseCases.Auth;

public class RefreshTokenUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenLifetimeProvider _accessTokenLifetimeProvider;
    private readonly IRefreshTokenLifetimeProvider _refreshTokenLifetimeProvider;

    public RefreshTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IUnitOfWork unitOfWork,
        IAccessTokenLifetimeProvider accessTokenLifetimeProvider,
        IRefreshTokenLifetimeProvider refreshTokenLifetimeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _unitOfWork = unitOfWork;
        _accessTokenLifetimeProvider = accessTokenLifetimeProvider;
        _refreshTokenLifetimeProvider = refreshTokenLifetimeProvider;
    }

    public async Task<Result<RefreshTokenResponseDto>> ExecuteAsync(RefreshTokenRequestDto request)
    {
        var refreshTokenHash = _refreshTokenHasher.Hash(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash);
        if (refreshToken is null || !refreshToken.IsActive)
            return Result<RefreshTokenResponseDto>.Failure("Refresh token inválido ou expirado.");

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
        if (user is null || !user.IsActive)
            return Result<RefreshTokenResponseDto>.Failure("Refresh token inválido ou expirado.");

        var newRefreshTokenValue = _refreshTokenGenerator.Generate();
        var newRefreshTokenHash = _refreshTokenHasher.Hash(newRefreshTokenValue);
        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var expiresInMinutes = _accessTokenLifetimeProvider.GetLifetimeInMinutes();
        var refreshTokenExpiresInDays = _refreshTokenLifetimeProvider.GetLifetimeInDays();
        var now = DateTime.UtcNow;
        var sessionId = refreshToken.SessionId == Guid.Empty ? Guid.NewGuid() : refreshToken.SessionId;
        var familyId = refreshToken.FamilyId == Guid.Empty ? Guid.NewGuid() : refreshToken.FamilyId;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            refreshToken.Revoked = now;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            await _refreshTokenRepository.CreateAsync(new RefreshToken
            {
                UserId = user.Id,
                SessionId = sessionId,
                FamilyId = familyId,
                TokenHash = newRefreshTokenHash,
                Created = now,
                Expires = now.AddDays(refreshTokenExpiresInDays)
            });
        });

        return Result<RefreshTokenResponseDto>.Success(new RefreshTokenResponseDto(
            accessToken,
            newRefreshTokenValue,
            expiresInMinutes));
    }
}
