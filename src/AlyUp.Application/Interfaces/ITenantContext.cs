namespace AlyUp.Application.Interfaces;

public interface ITenantContext
{
    bool ShouldApplyTenantFilter { get; }
    Guid? SalonId { get; }
}