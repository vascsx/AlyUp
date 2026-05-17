using AlyUp.Application.Security;
using AlyUp.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace AlyUp.Infrastructure.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.RequireMaster, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.IsInRole(UserRole.Admin.ToString()) &&
                    string.Equals(
                        context.User.FindFirst(AppClaimTypes.IsMaster)?.Value,
                        bool.TrueString,
                        StringComparison.OrdinalIgnoreCase));
            });

            options.AddPolicy(AppPolicies.RequireSalonOwnerOrMaster, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.IsInRole(UserRole.SalonOwner.ToString()) ||
                    (context.User.IsInRole(UserRole.Admin.ToString()) &&
                     string.Equals(
                         context.User.FindFirst(AppClaimTypes.IsMaster)?.Value,
                         bool.TrueString,
                         StringComparison.OrdinalIgnoreCase)));
            });

            options.AddPolicy(AppPolicies.RequireProfessional, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(UserRole.Professional.ToString());
            });
        });

        return services;
    }
}
