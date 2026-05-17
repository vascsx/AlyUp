using AlyUp.Application.Interfaces;
using AlyUp.Domain.Enums;

namespace AlyUp.Infrastructure.Security;

public class AccessScopeService : IAccessScopeService
{
    private readonly ICurrentUserService _currentUserService;

    public AccessScopeService(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public Guid? ResolveSalonScope(Guid? requestedSalonId = null)
    {
        if (_currentUserService.IsInRole(UserRole.Master))
        {
            return requestedSalonId;
        }

        if (_currentUserService.IsInRole(UserRole.SalonOwner) || _currentUserService.IsInRole(UserRole.Professional))
        {
            return _currentUserService.SalonId;
        }

        return null;
    }

    public bool CanAccessUser(Guid userId)
    {
        return _currentUserService.UserId == userId;
    }
}
