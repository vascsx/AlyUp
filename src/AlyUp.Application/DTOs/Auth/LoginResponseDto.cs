using AlyUp.Domain.Enums;

namespace AlyUp.Application.DTOs.Auth;

public record LoginResponseDto(
    string Token,
    Guid UserId,
    string Name,
    UserRole Role,
    Guid? SalonId);