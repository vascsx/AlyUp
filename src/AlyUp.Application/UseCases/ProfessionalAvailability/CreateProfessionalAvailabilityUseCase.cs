using AlyUp.Application.Common;
using AlyUp.Application.DTOs.ProfessionalAvailability;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.ProfessionalAvailability;

public class CreateProfessionalAvailabilityUseCase
{
    private readonly IProfessionalAvailabilityRepository _availabilityRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateProfessionalAvailabilityUseCase(
        IProfessionalAvailabilityRepository availabilityRepository,
        IProfessionalRepository professionalRepository,
        ICurrentUserService currentUserService)
    {
        _availabilityRepository = availabilityRepository;
        _professionalRepository = professionalRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ProfessionalAvailabilityResponseDto>> ExecuteAsync(Guid professionalId, CreateProfessionalAvailabilityRequestDto request)
    {
        if (!_currentUserService.IsInRole(UserRole.Master) && !_currentUserService.IsInRole(UserRole.SalonOwner))
        {
            return Result<ProfessionalAvailabilityResponseDto>.Failure("Usuário não autorizado.");
        }

        var professionalResult = await GetProfessionalAsync(professionalId);
        if (!professionalResult.IsSuccess)
        {
            return Result<ProfessionalAvailabilityResponseDto>.Failure(professionalResult.Error!);
        }

        var professional = professionalResult.Value!;

        var duplicateExists = await _availabilityRepository.ExistsExactAsync(
            professional.Id,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime);

        if (duplicateExists)
        {
            return Result<ProfessionalAvailabilityResponseDto>.Failure("Já existe uma disponibilidade cadastrada para o mesmo horário.");
        }

        var overlapExists = await _availabilityRepository.HasOverlapAsync(
            professional.Id,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime);

        if (overlapExists)
        {
            return Result<ProfessionalAvailabilityResponseDto>.Failure("Já existe uma disponibilidade com conflito de horário para este profissional.");
        }

        var availability = new AlyUp.Domain.Entities.ProfessionalAvailability
        {
            Id = Guid.NewGuid(),
            ProfessionalId = professional.Id,
            SalonId = professional.SalonId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _availabilityRepository.CreateAsync(availability);

        return Result<ProfessionalAvailabilityResponseDto>.Success(Map(availability));
    }

    private async Task<Result<Professional>> GetProfessionalAsync(Guid professionalId)
    {
        var professional = await _professionalRepository.GetByIdAsync(professionalId);
        if (professional is null || !professional.IsActive)
        {
            return Result<Professional>.Failure("O profissional informado não foi encontrado.");
        }

        if (_currentUserService.IsInRole(UserRole.SalonOwner) && _currentUserService.SalonId != professional.SalonId)
        {
            return Result<Professional>.Failure("O profissional informado não pertence ao salão do usuário autenticado.");
        }

        return Result<Professional>.Success(professional);
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
