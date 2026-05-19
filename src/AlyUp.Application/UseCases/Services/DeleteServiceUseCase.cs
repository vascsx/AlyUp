using AlyUp.Application.Common;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.Services;

public class DeleteServiceUseCase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteServiceUseCase(
        IServiceRepository serviceRepository,
        ICurrentUserService currentUserService)
    {
        _serviceRepository = serviceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result> ExecuteAsync(Guid serviceId)
    {
        if (!_currentUserService.IsInRole(UserRole.Master) && !_currentUserService.IsInRole(UserRole.SalonOwner))
        {
            return Result.Failure("Usuário não autorizado.");
        }

        var service = await _serviceRepository.GetByIdAsync(serviceId);
        if (service is null)
        {
            return Result.Failure("Serviço não encontrado.");
        }

        if (!service.IsActive)
        {
            return Result.Success();
        }

        service.IsActive = false;
        await _serviceRepository.UpdateAsync(service);

        return Result.Success();
    }
}
