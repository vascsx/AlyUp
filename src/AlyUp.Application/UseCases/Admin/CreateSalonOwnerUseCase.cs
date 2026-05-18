using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using SalonEntity = AlyUp.Domain.Entities.Salon;

namespace AlyUp.Application.UseCases.Admin;

public class CreateSalonOwnerUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ISalonRepository _salonRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSalonOwnerUseCase(
        IUserRepository userRepository,
        ISalonRepository salonRepository,
        IPasswordHasher passwordHasher,
        IInputNormalizer inputNormalizer,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _salonRepository = salonRepository;
        _passwordHasher = passwordHasher;
        _inputNormalizer = inputNormalizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> ExecuteAsync(CreateSalonOwnerRequestDto request)
    {
        var normalizedEmail = _inputNormalizer.NormalizeEmail(request.Email);

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
            return Result<Guid>.Failure("Já existe uma conta cadastrada com este e-mail.");

        if (await _salonRepository.ExistsBySalonDocumentAsync(request.SalonDocument))
            return Result<Guid>.Failure("Já existe um salão cadastrado com este documento.");

        var salonId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var salon = new SalonEntity
        {
            Id = salonId,
            Name = request.Name,
            Document = request.SalonDocument,
            Address = request.SalonAddress,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = userId,
            Name = request.Name,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.SalonOwner,
            SalonId = salonId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _salonRepository.CreateAsync(salon);
            await _userRepository.CreateAsync(user);
        });

        return Result<Guid>.Success(userId);
    }
}
