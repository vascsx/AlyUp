using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Services;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.Services;

public class GetServiceByIdUseCase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetServiceByIdUseCase(
        IServiceRepository serviceRepository,
        ICurrentUserService currentUserService)
    {
        _serviceRepository = serviceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ServiceResponseDto>> ExecuteAsync(Guid serviceId)
    {
        if (_currentUserService.Role is not (UserRole.Master or UserRole.SalonOwner or UserRole.Professional or UserRole.Client))
        {
            return Result<ServiceResponseDto>.Failure("Usuário não autorizado.");
        }

        var ignoreQueryFilters = _currentUserService.IsInRole(UserRole.Client);
        var service = await _serviceRepository.GetByIdAsync(serviceId, ignoreQueryFilters);

        if (service is null)
        {
            return Result<ServiceResponseDto>.Failure("Serviço não encontrado.");
        }

        if (_currentUserService.IsInRole(UserRole.Client) && !service.IsActive)
        {
            return Result<ServiceResponseDto>.Failure("Serviço não encontrado.");
        }

        return Result<ServiceResponseDto>.Success(Map(service));
    }

    private static ServiceResponseDto Map(Service service) =>
        new(
            service.Id,
            service.SalonId,
            service.Name,
            service.Description,
            service.DurationInMinutes,
            service.Price,
            service.IsActive,
            service.CreatedAt,
            service.UpdatedAt);
}
