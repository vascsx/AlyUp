namespace AlyUp.Application.UseCases.Professionals;

using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using System.Text.RegularExpressions;

public class CreateProfessionalUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISalonRepository _salonRepository;
    private readonly IInputNormalizer _inputNormalizer;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProfessionalUseCase(
        IUserRepository userRepository,
        IProfessionalRepository professionalRepository,
        IPasswordHasher passwordHasher,
        ISalonRepository salonRepository,
        IInputNormalizer inputNormalizer,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _professionalRepository = professionalRepository;
        _passwordHasher = passwordHasher;
        _salonRepository = salonRepository;
        _inputNormalizer = inputNormalizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> ExecuteAsync(CreateProfessionalRequestDto request, Guid salonId)
    {
        var normalizedEmail = _inputNormalizer.NormalizeEmail(request.Email);
        var normalizedDocument = Regex.Replace(request.Document, "\\D", string.Empty);

        if (await _salonRepository.GetByIdAsync(salonId) is null)
            return Result<Guid>.Failure("O salão informado não foi encontrado.");

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
            return Result<Guid>.Failure("Já existe um profissional cadastrado com este e-mail.");

        var professionalId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user = new User
            {
                Id = professionalId,
                Name = request.Name,
                Email = normalizedEmail,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Role = UserRole.Professional,
                SalonId = salonId,
                IsActive = true,
                CreatedAt = createdAt
            };

            var professional = new Professional
            {
                Id = professionalId,
                SalonId = salonId,
                Name = request.Name,
                Email = normalizedEmail,
                Document = normalizedDocument,
                IsActive = true,
                CreatedAt = createdAt
            };

            await _userRepository.CreateAsync(user);
            await _professionalRepository.CreateAsync(professional);
        });

        return Result<Guid>.Success(professionalId);
    }
}
