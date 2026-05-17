using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;

namespace AlyUp.Application.UseCases.Auth;

public class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IInputNormalizer _inputNormalizer;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IInputNormalizer inputNormalizer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _inputNormalizer = inputNormalizer;
    }

    public async Task<Result<LoginResponseDto>> ExecuteAsync(LoginRequestDto request)
    {
        var email = _inputNormalizer.NormalizeEmail(request.Email);

        var user = await _userRepository.GetByEmailAsync(email);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponseDto>.Failure("Email ou senha invalidos.");

        if (!user.IsActive)
            return Result<LoginResponseDto>.Failure("Email ou senha invalidos.");

        var token = _jwtTokenGenerator.GenerateToken(user);

        var response = new LoginResponseDto(
            token,
            user.Id,
            user.Name,
            user.Role,
            user.SalonId
        );

        return Result<LoginResponseDto>.Success(response);
    }
}
