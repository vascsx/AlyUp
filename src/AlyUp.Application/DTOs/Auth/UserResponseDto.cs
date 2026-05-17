using AlyUp.Domain.Enums;

namespace AlyUp.Application.DTOs.Auth;

public record UserResponseDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    Guid? SalonId);