using AlyUp.Domain.Enums;

namespace AlyUp.Application.DTOs.Auth;

public record LoginResponseDto(
    string Token,
    string RefreshToken,
    int ExpiresInMinutes,
    Guid UserId,
    string Name,
    UserRole Role,
    Guid? SalonId);
