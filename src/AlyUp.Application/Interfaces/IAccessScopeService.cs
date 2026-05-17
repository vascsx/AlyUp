namespace AlyUp.Application.Interfaces;

public interface IAccessScopeService
{
    Guid? ResolveSalonScope(Guid? requestedSalonId = null);
    bool CanAccessUser(Guid userId);
}
