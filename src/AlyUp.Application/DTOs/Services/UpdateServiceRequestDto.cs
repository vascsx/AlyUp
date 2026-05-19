namespace AlyUp.Application.DTOs.Services;

public record UpdateServiceRequestDto(
    string Name,
    string? Description,
    int DurationInMinutes,
    decimal Price);
