using AlyUp.Domain.Entities;
using AlyUp.Application.Interfaces;
using AlyUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlyUp.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _context;

    public ClientRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(Client client)
    {
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Client>> GetAllBySalonIdAsync(Guid salonId)
    {
        return await _context.Clients
            .Where(client => client.SalonId == salonId)
            .OrderBy(client => client.Name)
            .ToListAsync();
    }

    public async Task<Client?> GetByIdAsync(Guid id, Guid salonId)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(client =>
                client.Id == id &&
                client.SalonId == salonId
            );
    }

    public async Task UpdateAsync(Client client)
    {
        _context.Clients.Update(client);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Client client)
    {
        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
    }
}
