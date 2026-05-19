namespace AlyUp.Application.DTOs.Services;

public record CreateServiceRequestDto(
    string Name,
    string? Description,
    int DurationInMinutes,
    decimal Price,
    Guid? SalonId = null);
