using AlyUp.Domain.Enums;

namespace AlyUp.Application.DTOs.Auth;

public record LoginResponseDto(
    Guid UserId,
    string Name,
    UserRole Role,
    Guid? SalonId,
    string Token,
    string RefreshToken,
    int ExpiresInMinutes
    );
