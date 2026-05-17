namespace AlyUp.Application.DTOs.Auth;

public record RefreshTokenResponseDto(
    string Token,
    string RefreshToken,
    int ExpiresInMinutes);
