using AlyUp.Domain.Entities;

namespace AlyUp.Application.Interfaces;

public interface IProfessionalAvailabilityRepository
{
    Task CreateAsync(ProfessionalAvailability availability);
    Task<ProfessionalAvailability?> GetByIdAsync(Guid id, bool ignoreQueryFilters = false);
    Task<IReadOnlyCollection<ProfessionalAvailability>> GetByProfessionalIdAsync(Guid professionalId, bool includeInactive = false, bool ignoreQueryFilters = false);
    Task<bool> ExistsExactAsync(Guid professionalId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, Guid? excludingAvailabilityId = null, bool ignoreQueryFilters = false);
    Task<bool> HasOverlapAsync(Guid professionalId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, Guid? excludingAvailabilityId = null, bool ignoreQueryFilters = false);
    Task UpdateAsync(ProfessionalAvailability availability);
}
