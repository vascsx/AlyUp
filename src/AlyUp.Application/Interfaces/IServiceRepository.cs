using AlyUp.Domain.Entities;

namespace AlyUp.Application.Interfaces;

public interface IServiceRepository
{
    Task CreateAsync(Service service);
    Task<Service?> GetByIdAsync(Guid id, bool ignoreQueryFilters = false);
    Task<IReadOnlyCollection<Service>> GetBySalonIdAsync(Guid salonId, bool includeInactive = false, bool ignoreQueryFilters = false);
    Task UpdateAsync(Service service);
}
