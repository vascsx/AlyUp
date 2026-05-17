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
                policy.RequireRole(UserRole.Master.ToString());
            });

            options.AddPolicy(AppPolicies.RequireSalonOwnerOrMaster, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.IsInRole(UserRole.SalonOwner.ToString()) ||
                    context.User.IsInRole(UserRole.Master.ToString()));
            });

            options.AddPolicy(AppPolicies.RequireProfessional, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(UserRole.Professional.ToString());
            });

            options.AddPolicy(AppPolicies.RequireClient, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(UserRole.Client.ToString());
            });
        });

        return services;
    }
}
