using AlyUp.Domain.Entities;

namespace AlyUp.Application.Interfaces;

public interface IProfessionalRepository
{
    Task<Professional?> GetByIdAsync(Guid id);
    Task CreateAsync(Professional professional);
}
