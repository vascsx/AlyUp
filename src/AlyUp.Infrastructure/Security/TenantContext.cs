using AlyUp.Application.Interfaces;
using AlyUp.Domain.Enums;

namespace AlyUp.Infrastructure.Security;

public class TenantContext : ITenantContext
{
    private readonly ICurrentUserService _currentUserService;

    public TenantContext(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public bool ShouldApplyTenantFilter =>
        _currentUserService.IsAuthenticated &&
        !_currentUserService.IsInRole(UserRole.Master) &&
        !_currentUserService.IsInRole(UserRole.Admin);

    public Guid? SalonId => _currentUserService.SalonId;
}