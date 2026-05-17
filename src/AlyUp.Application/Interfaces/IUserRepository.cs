using AlyUp.Domain.Entities;

namespace AlyUp.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);
}
