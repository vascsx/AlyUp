namespace AlyUp.Application.DTOs.Auth;

public record CreateSalonOwnerRequestDto(
    string Name,
    string Email,
    string Password,
    string SalonName,
    string SalonDocument,
    string SalonAddress);