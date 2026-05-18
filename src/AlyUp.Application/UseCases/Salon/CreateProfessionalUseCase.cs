namespace AlyUp.Application.UseCases.Salon;

using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;

public class CreateProfessionalUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISalonRepository _salonRepository;
    private readonly IInputNormalizer _inputNormalizer;

    public CreateProfessionalUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISalonRepository salonRepository,
        IInputNormalizer inputNormalizer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _salonRepository = salonRepository;
        _inputNormalizer = inputNormalizer;
    }

    public async Task<Result<Guid>> ExecuteAsync(CreateProfessionalRequestDto request, Guid salonId)
    {
        var normalizedEmail = _inputNormalizer.NormalizeEmail(request.Email);

        if (await _salonRepository.GetByIdAsync(salonId) is null)
            return Result<Guid>.Failure("O salão informado não foi encontrado.");

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
            return Result<Guid>.Failure("Já existe um profissional cadastrado com este e-mail.");

        var professionalId = Guid.NewGuid();

        var user = new User
        {
            Id = professionalId,
            Name = request.Name,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Professional,
            SalonId = salonId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        return Result<Guid>.Success(professionalId);
    }
}