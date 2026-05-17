using AlyUp.Domain.Entities;

namespace AlyUp.Application.Interfaces;

public interface ISalonRepository
{
    Task CreateAsync(Salon salon);
    Task<Salon?> GetByIdAsync(Guid id);
    Task<List<Salon>> GetAllAsync();
    Task<bool> ExistsBySalonDocumentAsync(string document);
}
