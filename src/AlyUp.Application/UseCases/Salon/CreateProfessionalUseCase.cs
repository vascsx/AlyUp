namespace AlyUp.Application.UseCases.Salon;
using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using AlyUp.Domain.Exceptions;
public class CreateProfessionalUseCase
{
    private readonly IUserRepository _userRepository;


    private readonly IPasswordHasher _passwordHasher;

    public CreateProfessionalUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> ExecuteAsync(CreateProfessionalRequestDto request, Guid salonId)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            return Result<Guid>.Failure("Email já cadastrado.");

        var professionalId = Guid.NewGuid();

        var user = new User
        {
            Id = professionalId,
            Name = request.Name,
            Email = request.Email,
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
