using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlyUp.Infrastructure.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _context;

    public ServiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(Service service)
    {
        await _context.Services.AddAsync(service);
        await _context.SaveChangesAsync();
    }

    public async Task<Service?> GetByIdAsync(Guid id, bool ignoreQueryFilters = false)
    {
        var query = ignoreQueryFilters
            ? _context.Services.IgnoreQueryFilters()
            : _context.Services.AsQueryable();

        return await query.FirstOrDefaultAsync(service => service.Id == id);
    }

    public async Task<IReadOnlyCollection<Service>> GetBySalonIdAsync(Guid salonId, bool includeInactive = false, bool ignoreQueryFilters = false)
    {
        var query = ignoreQueryFilters
            ? _context.Services.IgnoreQueryFilters()
            : _context.Services.AsQueryable();

        query = query.Where(service => service.SalonId == salonId);

        if (!includeInactive)
        {
            query = query.Where(service => service.IsActive);
        }

        return await query
            .OrderBy(service => service.Name)
            .ToArrayAsync();
    }

    public async Task UpdateAsync(Service service)
    {
        _context.Services.Update(service);
        await _context.SaveChangesAsync();
    }
}
