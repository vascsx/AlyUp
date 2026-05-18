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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenLifetimeProvider _accessTokenLifetimeProvider;

    public RefreshTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork,
        IAccessTokenLifetimeProvider accessTokenLifetimeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _unitOfWork = unitOfWork;
        _accessTokenLifetimeProvider = accessTokenLifetimeProvider;
    }

    public async Task<Result<RefreshTokenResponseDto>> ExecuteAsync(RefreshTokenRequestDto request)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (refreshToken is null || !refreshToken.IsActive)
            return Result<RefreshTokenResponseDto>.Failure("Refresh token invalido ou expirado.");

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
        if (user is null || !user.IsActive)
            return Result<RefreshTokenResponseDto>.Failure("Refresh token invalido ou expirado.");

        var newRefreshTokenValue = _refreshTokenGenerator.Generate();
        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var expiresInMinutes = _accessTokenLifetimeProvider.GetLifetimeInMinutes();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            refreshToken.Revoked = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            await _refreshTokenRepository.CreateAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenValue,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(30)
            });
        });

        return Result<RefreshTokenResponseDto>.Success(new RefreshTokenResponseDto(
            accessToken,
            newRefreshTokenValue,
            expiresInMinutes));
    }
}
