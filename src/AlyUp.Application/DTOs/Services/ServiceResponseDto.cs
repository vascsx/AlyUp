namespace AlyUp.Application.DTOs.Services;

public record ServiceResponseDto(
    Guid Id,
    Guid SalonId,
    string Name,
    string Description,
    int DurationInMinutes,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
