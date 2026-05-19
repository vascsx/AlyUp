namespace AlyUp.Application.DTOs.ProfessionalAvailability;

public record ProfessionalAvailabilityResponseDto(
    Guid Id,
    Guid ProfessionalId,
    Guid SalonId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
