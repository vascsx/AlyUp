using SalonEntity = AlyUp.Domain.Entities.Salon;
using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
namespace AlyUp.Application.UseCases.Admin;

public class CreateSalonOwnerUseCase

{
    private readonly IUserRepository _userRepository;
    private readonly ISalonRepository _salonRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateSalonOwnerUseCase(
        IUserRepository userRepository,
        ISalonRepository salonRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _salonRepository = salonRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> ExecuteAsync(CreateSalonOwnerRequestDto request)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            return Result<Guid>.Failure("Email já cadastrado.");

        if (await _salonRepository.ExistsBySalonDocumentAsync(request.SalonDocument))
            return Result<Guid>.Failure("Documento do salão já cadastrado.");

        var salonId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var salon = new SalonEntity
        {
            Id = salonId,
            Name = request.SalonName,
            Document = request.SalonDocument,
            Address = request.SalonAddress,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = userId,
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.SalonOwner,
            SalonId = salonId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _salonRepository.CreateAsync(salon);
        await _userRepository.CreateAsync(user);

        return Result<Guid>.Success(userId);
    }
}
