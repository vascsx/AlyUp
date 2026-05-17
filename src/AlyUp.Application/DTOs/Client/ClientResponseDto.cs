namespace AlyUp.Application.DTOs.Client;

public record ClientResponseDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    string? Notes);