using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Services;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.Services;

public class CreateServiceUseCase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ISalonRepository _salonRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAccessScopeService _accessScopeService;
    private readonly IInputNormalizer _inputNormalizer;

    public CreateServiceUseCase(
        IServiceRepository serviceRepository,
        ISalonRepository salonRepository,
        ICurrentUserService currentUserService,
        IAccessScopeService accessScopeService,
        IInputNormalizer inputNormalizer)
    {
        _serviceRepository = serviceRepository;
        _salonRepository = salonRepository;
        _currentUserService = currentUserService;
        _accessScopeService = accessScopeService;
        _inputNormalizer = inputNormalizer;
    }

    public async Task<Result<ServiceResponseDto>> ExecuteAsync(CreateServiceRequestDto request)
    {
        if (!_currentUserService.IsInRole(UserRole.Master) && !_currentUserService.IsInRole(UserRole.SalonOwner))
        {
            return Result<ServiceResponseDto>.Failure("Usuário não autorizado.");
        }

        var salonId = _accessScopeService.ResolveSalonScope(request.SalonId);
        if (!salonId.HasValue || salonId.Value == Guid.Empty)
        {
            return Result<ServiceResponseDto>.Failure("Não foi possível identificar o salão responsável pelo serviço.");
        }

        if (await _salonRepository.GetByIdAsync(salonId.Value) is null)
        {
            return Result<ServiceResponseDto>.Failure("O salão informado não foi encontrado.");
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            SalonId = salonId.Value,
            Name = _inputNormalizer.NormalizeText(request.Name),
            Description = _inputNormalizer.NormalizeNullableText(request.Description),
            DurationInMinutes = request.DurationInMinutes,
            Price = request.Price,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _serviceRepository.CreateAsync(service);

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
