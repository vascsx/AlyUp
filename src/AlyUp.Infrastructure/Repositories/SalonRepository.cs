using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Exceptions;
using AlyUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        try
        {
            await _context.Salons.AddAsync(salon);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new SalonDocumentAlreadyExistsException(salon.Document);
        }
    }

    public async Task<Salon?> GetByIdAsync(Guid id) =>
        await _context.Salons.FindAsync(id);

    public async Task<List<Salon>> GetAllAsync() =>
        await _context.Salons.ToListAsync();

    public async Task<bool> ExistsBySalonDocumentAsync(string document) =>
        await _context.Salons.AnyAsync(salon => salon.Document == document);
}
