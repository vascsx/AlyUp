namespace AlyUp.Application.DTOs.Auth;

public record CreateProfessionalRequestDto(string Name, string Email, string Password, string Document, Guid? SalonId = null);
