using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Services;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.Services;

public class UpdateServiceUseCase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IInputNormalizer _inputNormalizer;

    public UpdateServiceUseCase(
        IServiceRepository serviceRepository,
        ICurrentUserService currentUserService,
        IInputNormalizer inputNormalizer)
    {
        _serviceRepository = serviceRepository;
        _currentUserService = currentUserService;
        _inputNormalizer = inputNormalizer;
    }

    public async Task<Result<ServiceResponseDto>> ExecuteAsync(Guid serviceId, UpdateServiceRequestDto request)
    {
        if (!_currentUserService.IsInRole(UserRole.Master) && !_currentUserService.IsInRole(UserRole.SalonOwner))
        {
            return Result<ServiceResponseDto>.Failure("Usuário não autorizado.");
        }

        var service = await _serviceRepository.GetByIdAsync(serviceId);
        if (service is null)
        {
            return Result<ServiceResponseDto>.Failure("Serviço não encontrado.");
        }

        service.Name = _inputNormalizer.NormalizeText(request.Name);
        service.Description = _inputNormalizer.NormalizeNullableText(request.Description);
        service.DurationInMinutes = request.DurationInMinutes;
        service.Price = request.Price;

        await _serviceRepository.UpdateAsync(service);

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
