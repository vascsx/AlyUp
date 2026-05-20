using AlyUp.Application.Common;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.ProfessionalAvailability;

public class DeleteProfessionalAvailabilityUseCase
{
    private readonly IProfessionalAvailabilityRepository _availabilityRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteProfessionalAvailabilityUseCase(
        IProfessionalAvailabilityRepository availabilityRepository,
        IProfessionalRepository professionalRepository,
        ICurrentUserService currentUserService)
    {
        _availabilityRepository = availabilityRepository;
        _professionalRepository = professionalRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result> ExecuteAsync(Guid professionalId, Guid availabilityId)
    {
        if (!_currentUserService.IsInRole(UserRole.Master) && !_currentUserService.IsInRole(UserRole.SalonOwner))
        {
            return Result.Failure("Usuário não autorizado.");
        }

        var professional = await _professionalRepository.GetByIdAsync(professionalId);
        if (professional is null || !professional.IsActive)
        {
            return Result.Failure("O profissional informado não foi encontrado.");
        }

        if (_currentUserService.IsInRole(UserRole.SalonOwner) && _currentUserService.SalonId != professional.SalonId)
        {
            return Result.Failure("O profissional informado não pertence ao salão do usuário autenticado.");
        }

        var availability = await _availabilityRepository.GetByIdAsync(availabilityId);
        if (availability is null || availability.ProfessionalId != professionalId)
        {
            return Result.Failure("Disponibilidade não encontrada.");
        }

        if (!availability.IsActive)
        {
            return Result.Success();
        }

        availability.IsActive = false;
        await _availabilityRepository.UpdateAsync(availability);

        return Result.Success();
    }
}
