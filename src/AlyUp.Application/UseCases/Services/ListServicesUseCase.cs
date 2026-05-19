using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Services;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.Services;

public class ListServicesUseCase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ISalonRepository _salonRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAccessScopeService _accessScopeService;

    public ListServicesUseCase(
        IServiceRepository serviceRepository,
        ISalonRepository salonRepository,
        ICurrentUserService currentUserService,
        IAccessScopeService accessScopeService)
    {
        _serviceRepository = serviceRepository;
        _salonRepository = salonRepository;
        _currentUserService = currentUserService;
        _accessScopeService = accessScopeService;
    }

    public async Task<Result<IReadOnlyCollection<ServiceResponseDto>>> ExecuteAsync(Guid? requestedSalonId, bool includeInactive)
    {
        if (_currentUserService.Role is not (UserRole.Master or UserRole.SalonOwner or UserRole.Professional or UserRole.Client))
        {
            return Result<IReadOnlyCollection<ServiceResponseDto>>.Failure("Usuário não autorizado.");
        }

        var salonIdResult = await ResolveSalonIdAsync(requestedSalonId);
        if (!salonIdResult.IsSuccess)
        {
            return Result<IReadOnlyCollection<ServiceResponseDto>>.Failure(salonIdResult.Error!);
        }

        var role = _currentUserService.Role;
        var ignoreQueryFilters = role == UserRole.Client;
        var effectiveIncludeInactive = role is UserRole.Master or UserRole.SalonOwner ? includeInactive : false;

        var services = await _serviceRepository.GetBySalonIdAsync(
            salonIdResult.Value!.Value,
            effectiveIncludeInactive,
            ignoreQueryFilters);

        return Result<IReadOnlyCollection<ServiceResponseDto>>.Success(
            services
                .Select(Map)
                .ToArray());
    }

    private async Task<Result<Guid?>> ResolveSalonIdAsync(Guid? requestedSalonId)
    {
        if (_currentUserService.IsInRole(UserRole.Client))
        {
            if (!requestedSalonId.HasValue || requestedSalonId.Value == Guid.Empty)
            {
                return Result<Guid?>.Failure("O salão deve ser informado para listar serviços.");
            }

            if (await _salonRepository.GetByIdAsync(requestedSalonId.Value) is null)
            {
                return Result<Guid?>.Failure("O salão informado não foi encontrado.");
            }

            return Result<Guid?>.Success(requestedSalonId.Value);
        }

        var salonId = _accessScopeService.ResolveSalonScope(requestedSalonId);
        if (!salonId.HasValue || salonId.Value == Guid.Empty)
        {
            return Result<Guid?>.Failure("Não foi possível identificar o salão responsável pelos serviços.");
        }

        if (await _salonRepository.GetByIdAsync(salonId.Value) is null)
        {
            return Result<Guid?>.Failure("O salão informado não foi encontrado.");
        }

        return Result<Guid?>.Success(salonId.Value);
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
