using AlyUp.Domain.Entities;
using AlyUp.Application.Interfaces;
using AlyUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlyUp.Infrastructure.Repositories;

public class SalonRepository : ISalonRepository
{
    private readonly AppDbContext _context;

    public SalonRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(Salon salon)
    {
        await _context.Salons.AddAsync(salon);
        await _context.SaveChangesAsync();
    }

    public async Task<Salon?> GetByIdAsync(Guid id) =>
        await _context.Salons.FindAsync(id);

    public async Task<List<Salon>> GetAllAsync() =>
        await _context.Salons.ToListAsync();

    public async Task<bool> ExistsBySalonDocumentAsync(string document) =>
       await _context.Salons.AnyAsync(u => u.Document == document);
}
