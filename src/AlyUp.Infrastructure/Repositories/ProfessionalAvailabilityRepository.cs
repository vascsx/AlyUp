using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlyUp.Infrastructure.Repositories;

public class ProfessionalAvailabilityRepository : IProfessionalAvailabilityRepository
{
    private readonly AppDbContext _context;

    public ProfessionalAvailabilityRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(ProfessionalAvailability availability)
    {
        await _context.ProfessionalAvailabilities.AddAsync(availability);
        await _context.SaveChangesAsync();
    }

    public async Task<ProfessionalAvailability?> GetByIdAsync(Guid id, bool ignoreQueryFilters = false)
    {
        var query = ignoreQueryFilters
            ? _context.ProfessionalAvailabilities.IgnoreQueryFilters()
            : _context.ProfessionalAvailabilities.AsQueryable();

        return await query.FirstOrDefaultAsync(availability => availability.Id == id);
    }

    public async Task<IReadOnlyCollection<ProfessionalAvailability>> GetByProfessionalIdAsync(
        Guid professionalId,
        bool includeInactive = false,
        bool ignoreQueryFilters = false)
    {
        var query = ignoreQueryFilters
            ? _context.ProfessionalAvailabilities.IgnoreQueryFilters()
            : _context.ProfessionalAvailabilities.AsQueryable();

        query = query.Where(availability => availability.ProfessionalId == professionalId);

        if (!includeInactive)
        {
            query = query.Where(availability => availability.IsActive);
        }

        return await query
            .OrderBy(availability => availability.DayOfWeek)
            .ThenBy(availability => availability.StartTime)
            .ToArrayAsync();
    }

    public async Task<bool> ExistsExactAsync(
        Guid professionalId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludingAvailabilityId = null,
        bool ignoreQueryFilters = false)
    {
        var query = ignoreQueryFilters
            ? _context.ProfessionalAvailabilities.IgnoreQueryFilters()
            : _context.ProfessionalAvailabilities.AsQueryable();

        query = query.Where(availability =>
            availability.ProfessionalId == professionalId &&
            availability.DayOfWeek == dayOfWeek &&
            availability.StartTime == startTime &&
            availability.EndTime == endTime &&
            availability.IsActive);

        if (excludingAvailabilityId.HasValue)
        {
            query = query.Where(availability => availability.Id != excludingAvailabilityId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> HasOverlapAsync(
        Guid professionalId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludingAvailabilityId = null,
        bool ignoreQueryFilters = false)
    {
        var query = ignoreQueryFilters
            ? _context.ProfessionalAvailabilities.IgnoreQueryFilters()
            : _context.ProfessionalAvailabilities.AsQueryable();

        query = query.Where(availability =>
            availability.ProfessionalId == professionalId &&
            availability.DayOfWeek == dayOfWeek &&
            availability.IsActive &&
            startTime < availability.EndTime &&
            endTime > availability.StartTime);

        if (excludingAvailabilityId.HasValue)
        {
            query = query.Where(availability => availability.Id != excludingAvailabilityId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task UpdateAsync(ProfessionalAvailability availability)
    {
        _context.ProfessionalAvailabilities.Update(availability);
        await _context.SaveChangesAsync();
    }
}
