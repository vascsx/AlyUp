using System.Security.Claims;
using AlyUp.Application.Interfaces;
using AlyUp.Application.Security;
using AlyUp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AlyUp.Infrastructure.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId => TryParseGuidClaim(AppClaimTypes.UserId) ?? TryParseGuidClaim(ClaimTypes.NameIdentifier);

    public Guid? SalonId => TryParseGuidClaim(AppClaimTypes.SalonId);

    public UserRole? Role
    {
        get
        {
            var rawRole = User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue(AppClaimTypes.Role);
            return Enum.TryParse<UserRole>(rawRole, out var role) ? role : null;
        }
    }

    public bool IsInRole(UserRole role) => Role == role;

    private Guid? TryParseGuidClaim(string claimType)
    {
        var value = User?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
