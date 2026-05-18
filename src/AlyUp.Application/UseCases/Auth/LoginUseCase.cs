using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;

namespace AlyUp.Application.UseCases.Auth;

public class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly IAccessTokenLifetimeProvider _accessTokenLifetimeProvider;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IInputNormalizer inputNormalizer,
        IAccessTokenLifetimeProvider accessTokenLifetimeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _inputNormalizer = inputNormalizer;
        _accessTokenLifetimeProvider = accessTokenLifetimeProvider;
    }

    public async Task<Result<LoginResponseDto>> ExecuteAsync(LoginRequestDto request)
    {
        var email = _inputNormalizer.NormalizeEmail(request.Email);

        var user = await _userRepository.GetByEmailAsync(email);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponseDto>.Failure("Email ou senha inválidos.");

        if (!user.IsActive)
            return Result<LoginResponseDto>.Failure("Email ou senha inválidos.");

        var token = _jwtTokenGenerator.GenerateToken(user);
        var refreshTokenValue = _refreshTokenGenerator.Generate();
        var expiresInMinutes = _accessTokenLifetimeProvider.GetLifetimeInMinutes();

        await _refreshTokenRepository.CreateAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(30)
        });

        var response = new LoginResponseDto(
            user.Id, 
            user.Name, 
            user.Role, 
            user.SalonId,
            token,
            refreshTokenValue,
            expiresInMinutes
        );

        return Result<LoginResponseDto>.Success(response);
    }
}
