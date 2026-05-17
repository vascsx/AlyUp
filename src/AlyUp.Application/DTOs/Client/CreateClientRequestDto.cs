namespace AlyUp.Application.DTOs.Client;

public record CreateClientRequestDto(
    string Name,
    string? Phone,
    string? Email,
    string? Notes);