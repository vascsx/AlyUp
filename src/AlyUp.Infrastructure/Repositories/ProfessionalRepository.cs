using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlyUp.Infrastructure.Repositories;

public class ProfessionalRepository : IProfessionalRepository
{
    private readonly AppDbContext _context;

    public ProfessionalRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Professional?> GetByIdAsync(Guid id) =>
        await _context.Professionals.FirstOrDefaultAsync(professional => professional.Id == id);

    public async Task CreateAsync(Professional professional)
    {
        await _context.Professionals.AddAsync(professional);
        await _context.SaveChangesAsync();
    }
}
