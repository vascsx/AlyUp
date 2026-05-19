namespace AlyUp.Application.DTOs.ProfessionalAvailability;

public record UpdateProfessionalAvailabilityRequestDto(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
