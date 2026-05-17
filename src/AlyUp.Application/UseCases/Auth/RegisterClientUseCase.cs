namespace AlyUp.Application.UseCases.Auth;

using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;

public class RegisterClientUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInputNormalizer _inputNormalizer;

    public RegisterClientUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IInputNormalizer inputNormalizer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _inputNormalizer = inputNormalizer;
    }

    public async Task<Result<Guid>> ExecuteAsync(RegisterClientRequestDto request)
    {
        var normalizedEmail = _inputNormalizer.NormalizeEmail(request.Email);
        var normalizedName = _inputNormalizer.NormalizeText(request.Name);

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
            return Result<Guid>.Failure("Email ja cadastrado.");

        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Name = normalizedName,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Client,
            SalonId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        return Result<Guid>.Success(userId);
    }
}
