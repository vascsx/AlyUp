using System.Text;
using System.Text.Json;
using AlyUp.Application.Interfaces;
using AlyUp.Application.Security;
using Microsoft.AspNetCore.Http;

namespace AlyUp.Infrastructure.Middleware;

public class AuthRateLimitContextMiddleware
{
    private readonly RequestDelegate _next;

    public AuthRateLimitContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IInputNormalizer inputNormalizer,
        IRefreshTokenHasher refreshTokenHasher)
    {
        if (IsAuthRateLimitedEndpoint(context.Request.Path, context.Request.Method))
        {
            await PopulateRateLimitContextAsync(context, inputNormalizer, refreshTokenHasher);
        }

        await _next(context);
    }

    private async Task PopulateRateLimitContextAsync(
        HttpContext context,
        IInputNormalizer inputNormalizer,
        IRefreshTokenHasher refreshTokenHasher)
    {
        if (!(context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return;
        }

        context.Request.EnableBuffering();

        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            using var document = JsonDocument.Parse(body);
            if (IsLoginOrRegisterClient(context.Request.Path))
            {
                if (TryReadString(document.RootElement, "email", out var email))
                {
                    context.Items[RateLimitKeys.Email] = inputNormalizer.NormalizeEmail(email);
                }
            }
            else if (IsRefreshOrLogout(context.Request.Path) && TryReadString(document.RootElement, "refreshToken", out var refreshToken))
            {
                context.Items[RateLimitKeys.RefreshTokenHash] = refreshTokenHasher.Hash(refreshToken);
            }
        }
        finally
        {
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
            }
        }
    }

    private static bool TryReadString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;

        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool IsAuthRateLimitedEndpoint(PathString path, string method) =>
        HttpMethods.IsPost(method) && path.StartsWithSegments("/api/Auth");

    private static bool IsLoginOrRegisterClient(PathString path) =>
        path.Equals("/api/Auth/login", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/api/Auth/registerClient", StringComparison.OrdinalIgnoreCase);

    private static bool IsRefreshOrLogout(PathString path) =>
        path.Equals("/api/Auth/refresh", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/api/Auth/logout", StringComparison.OrdinalIgnoreCase);

    private static class RateLimitKeys
    {
        public const string Email = "auth-rate-limit-email";
        public const string RefreshTokenHash = "auth-rate-limit-refresh-token-hash";
    }
}