using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Client;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Exceptions;
namespace AlyUp.Application.UseCases.Clients;

public class CreateClientUseCase
{
    private readonly IClientRepository _clientRepository;



    public CreateClientUseCase(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<Result<Guid>> ExecuteAsync(CreateClientRequestDto request, Guid salonId)
    {
        var clientId = Guid.NewGuid();

        var client = new Client
        {
            Id = clientId,
            SalonId = salonId,
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim().ToLower(),
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _clientRepository.CreateAsync(client);

        return Result<Guid>.Success(clientId);
    }
}
