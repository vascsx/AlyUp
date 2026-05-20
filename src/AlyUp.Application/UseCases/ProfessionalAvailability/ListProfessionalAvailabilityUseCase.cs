using AlyUp.Application.Common;
using AlyUp.Application.DTOs.ProfessionalAvailability;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.ProfessionalAvailability;

public class ListProfessionalAvailabilityUseCase
{
    private readonly IProfessionalAvailabilityRepository _availabilityRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly ICurrentUserService _currentUserService;

    public ListProfessionalAvailabilityUseCase(
        IProfessionalAvailabilityRepository availabilityRepository,
        IProfessionalRepository professionalRepository,
        ICurrentUserService currentUserService)
    {
        _availabilityRepository = availabilityRepository;
        _professionalRepository = professionalRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyCollection<ProfessionalAvailabilityResponseDto>>> ExecuteAsync(Guid professionalId, bool includeInactive)
    {
        var professional = await _professionalRepository.GetByIdAsync(professionalId);
        if (professional is null || !professional.IsActive)
        {
            return Result<IReadOnlyCollection<ProfessionalAvailabilityResponseDto>>.Failure("O profissional informado não foi encontrado.");
        }

        var role = _currentUserService.Role;
        if (role is not (UserRole.Master or UserRole.SalonOwner or UserRole.Professional or UserRole.Client))
        {
            return Result<IReadOnlyCollection<ProfessionalAvailabilityResponseDto>>.Failure("Usuário não autorizado.");
        }

        if (role == UserRole.SalonOwner && _currentUserService.SalonId != professional.SalonId)
        {
            return Result<IReadOnlyCollection<ProfessionalAvailabilityResponseDto>>.Failure("Usuário não autorizado.");
        }

        if (role == UserRole.Professional && _currentUserService.UserId != professionalId)
        {
            return Result<IReadOnlyCollection<ProfessionalAvailabilityResponseDto>>.Failure("Usuário não autorizado.");
        }

        var ignoreQueryFilters = role == UserRole.Client;
        var effectiveIncludeInactive = role is UserRole.Master or UserRole.SalonOwner ? includeInactive : false;

        var availabilities = await _availabilityRepository.GetByProfessionalIdAsync(
            professionalId,
            effectiveIncludeInactive,
            ignoreQueryFilters);

        return Result<IReadOnlyCollection<ProfessionalAvailabilityResponseDto>>.Success(
            availabilities.Select(Map).ToArray());
    }

    private static ProfessionalAvailabilityResponseDto Map(AlyUp.Domain.Entities.ProfessionalAvailability availability) =>
        new(
            availability.Id,
            availability.ProfessionalId,
            availability.SalonId,
            availability.DayOfWeek,
            availability.StartTime,
            availability.EndTime,
            availability.IsActive,
            availability.CreatedAt,
            availability.UpdatedAt);
}
