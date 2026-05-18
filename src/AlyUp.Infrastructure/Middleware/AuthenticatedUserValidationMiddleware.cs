using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AlyUp.Application.Interfaces;
using AlyUp.Application.Security;
using AlyUp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AlyUp.Infrastructure.Middleware;

public class AuthenticatedUserValidationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticatedUserValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        if (!TryReadTokenClaims(context.User, out var tokenState))
        {
            await RejectAsync(context);
            return;
        }

        var user = await userRepository.GetByIdAsync(tokenState.UserId);
        if (user is null ||
            !user.IsActive ||
            user.Role != tokenState.Role ||
            user.SalonId != tokenState.SalonId ||
            IsTokenRevokedByUserUpdate(user.UpdatedAt, tokenState.IssuedAt))
        {
            await RejectAsync(context);
            return;
        }

        await _next(context);
    }

    private static bool TryReadTokenClaims(ClaimsPrincipal principal, out TokenState tokenState)
    {
        tokenState = default;

        var userIdClaim = principal.FindFirstValue(AppClaimTypes.UserId)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return false;
        }

        var roleClaim = principal.FindFirstValue(ClaimTypes.Role)
            ?? principal.FindFirstValue(AppClaimTypes.Role);
        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return false;
        }

        var issuedAtClaim = principal.FindFirstValue(AppClaimTypes.TokenIssuedAt);
        if (!long.TryParse(issuedAtClaim, out var issuedAtTicks))
        {
            return false;
        }

        Guid? salonId = null;
        var salonIdClaim = principal.FindFirstValue(AppClaimTypes.SalonId);
        if (!string.IsNullOrWhiteSpace(salonIdClaim))
        {
            if (!Guid.TryParse(salonIdClaim, out var parsedSalonId))
            {
                return false;
            }

            salonId = parsedSalonId;
        }

        tokenState = new TokenState(userId, role, salonId, new DateTime(issuedAtTicks, DateTimeKind.Utc));
        return true;
    }

    private static bool IsTokenRevokedByUserUpdate(DateTime? userUpdatedAt, DateTime tokenIssuedAt)
    {
        return userUpdatedAt.HasValue && userUpdatedAt.Value.ToUniversalTime() > tokenIssuedAt;
    }

    private static Task RejectAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            message = "Token inválido ou usuário não autorizado."
        }));
    }

    private readonly record struct TokenState(
        Guid UserId,
        UserRole Role,
        Guid? SalonId,
        DateTime IssuedAt);
}
