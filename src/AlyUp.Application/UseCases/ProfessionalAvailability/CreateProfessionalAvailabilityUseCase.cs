using AlyUp.Application.Common;
using AlyUp.Application.DTOs.ProfessionalAvailability;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.ProfessionalAvailability;

public class CreateProfessionalAvailabilityUseCase
{
    private readonly IProfessionalAvailabilityRepository _availabilityRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateProfessionalAvailabilityUseCase(
        IProfessionalAvailabilityRepository availabilityRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _availabilityRepository = availabilityRepository;
        _userRepository = userRepository;
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
            SalonId = professional.SalonId!.Value,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _availabilityRepository.CreateAsync(availability);

        return Result<ProfessionalAvailabilityResponseDto>.Success(Map(availability));
    }

    private async Task<Result<User>> GetProfessionalAsync(Guid professionalId)
    {
        var professional = await _userRepository.GetByIdAsync(professionalId);
        if (professional is null || professional.Role != UserRole.Professional || !professional.SalonId.HasValue)
        {
            return Result<User>.Failure("O profissional informado não foi encontrado.");
        }

        if (_currentUserService.IsInRole(UserRole.SalonOwner) && _currentUserService.SalonId != professional.SalonId)
        {
            return Result<User>.Failure("O profissional informado não pertence ao salão do usuário autenticado.");
        }

        return Result<User>.Success(professional);
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
