using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using AlyUp.Application.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace AlyUp.Infrastructure.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(new
                    {
                        message = "Muitas tentativas. Tente novamente em instantes."
                    }),
                    cancellationToken);
            };

            options.AddPolicy(AppRateLimitPolicies.AuthLogin, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, AppRateLimitPolicies.AuthLogin),
                    _ => CreateOptions(5, TimeSpan.FromMinutes(1))));

            options.AddPolicy(AppRateLimitPolicies.AuthRefresh, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, AppRateLimitPolicies.AuthRefresh),
                    _ => CreateOptions(10, TimeSpan.FromMinutes(1))));

            options.AddPolicy(AppRateLimitPolicies.AuthLogout, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, AppRateLimitPolicies.AuthLogout),
                    _ => CreateOptions(10, TimeSpan.FromMinutes(1))));

            options.AddPolicy(AppRateLimitPolicies.AuthRegisterClient, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext, AppRateLimitPolicies.AuthRegisterClient),
                    _ => CreateOptions(3, TimeSpan.FromMinutes(5))));
        });

        return services;
    }

    private static FixedWindowRateLimiterOptions CreateOptions(int permitLimit, TimeSpan window) =>
        new()
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        };

    private static string GetPartitionKey(HttpContext context, string policyName)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var flowIdentifier = policyName switch
        {
            AppRateLimitPolicies.AuthLogin => GetContextValue(context, AuthRateLimitContextKeys.Email) ?? "unknown",
            AppRateLimitPolicies.AuthRegisterClient => GetContextValue(context, AuthRateLimitContextKeys.Email) ?? "unknown",
            AppRateLimitPolicies.AuthRefresh => GetContextValue(context, AuthRateLimitContextKeys.RefreshTokenHash) ?? "unknown",
            AppRateLimitPolicies.AuthLogout => GetContextValue(context, AuthRateLimitContextKeys.RefreshTokenHash) ?? "unknown",
            _ => "unknown"
        };

        return $"{policyName}:{remoteIp ?? "unknown"}:{flowIdentifier}";
    }

    private static string? GetContextValue(HttpContext context, string key) =>
        context.Items.TryGetValue(key, out var value) ? value as string : null;

    private static class AuthRateLimitContextKeys
    {
        public const string Email = "auth-rate-limit-email";
        public const string RefreshTokenHash = "auth-rate-limit-refresh-token-hash";
    }
}
