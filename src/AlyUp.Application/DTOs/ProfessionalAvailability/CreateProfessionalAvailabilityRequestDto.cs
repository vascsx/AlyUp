namespace AlyUp.Application.DTOs.ProfessionalAvailability;

public record CreateProfessionalAvailabilityRequestDto(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
