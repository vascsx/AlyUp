using AlyUp.Domain.Entities;

namespace AlyUp.Application.Interfaces;

public interface IClientRepository
{
    Task CreateAsync(Client client);
    Task<List<Client>> GetAllBySalonIdAsync(Guid salonId);
    Task<Client?> GetByIdAsync(Guid id, Guid salonId);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Client client);
}
