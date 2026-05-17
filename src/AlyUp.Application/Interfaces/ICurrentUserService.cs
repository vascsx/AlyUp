using AlyUp.Domain.Enums;

namespace AlyUp.Application.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? SalonId { get; }
    UserRole? Role { get; }
    bool IsInRole(UserRole role);
}
