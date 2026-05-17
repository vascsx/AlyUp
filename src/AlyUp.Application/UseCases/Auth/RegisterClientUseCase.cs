namespace AlyUp.Application.UseCases.Auth;
using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using AlyUp.Domain.Exceptions;

public class RegisterClientUseCase
{
    private readonly IUserRepository _userRepository;


    private readonly IPasswordHasher _passwordHasher;

    public RegisterClientUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> ExecuteAsync(RegisterClientRequestDto request)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            return Result<Guid>.Failure("Email já cadastrado.");

        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLower(),
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
