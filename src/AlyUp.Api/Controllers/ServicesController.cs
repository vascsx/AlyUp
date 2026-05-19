using AlyUp.Application.DTOs.Services;
using AlyUp.Application.Security;
using AlyUp.Application.UseCases.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly CreateServiceUseCase _createServiceUseCase;
    private readonly ListServicesUseCase _listServicesUseCase;
    private readonly GetServiceByIdUseCase _getServiceByIdUseCase;
    private readonly UpdateServiceUseCase _updateServiceUseCase;
    private readonly DeleteServiceUseCase _deleteServiceUseCase;

    public ServicesController(
        CreateServiceUseCase createServiceUseCase,
        ListServicesUseCase listServicesUseCase,
        GetServiceByIdUseCase getServiceByIdUseCase,
        UpdateServiceUseCase updateServiceUseCase,
        DeleteServiceUseCase deleteServiceUseCase)
    {
        _createServiceUseCase = createServiceUseCase;
        _listServicesUseCase = listServicesUseCase;
        _getServiceByIdUseCase = getServiceByIdUseCase;
        _updateServiceUseCase = updateServiceUseCase;
        _deleteServiceUseCase = deleteServiceUseCase;
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireSalonOwnerOrMaster)]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestDto request)
    {
        var result = await _createServiceUseCase.ExecuteAsync(request);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? salonId = null, [FromQuery] bool includeInactive = false)
    {
        var result = await _listServicesUseCase.ExecuteAsync(salonId, includeInactive);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getServiceByIdUseCase.ExecuteAsync(id);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireSalonOwnerOrMaster)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequestDto request)
    {
        var result = await _updateServiceUseCase.ExecuteAsync(id, request);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.RequireSalonOwnerOrMaster)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _deleteServiceUseCase.ExecuteAsync(id);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error);
        }

        return NoContent();
    }

    private IActionResult MapFailure(string? error)
    {
        if (string.Equals(error, "Usuário não autorizado.", StringComparison.Ordinal))
        {
            return Forbid();
        }

        if (string.Equals(error, "Serviço não encontrado.", StringComparison.Ordinal) ||
            string.Equals(error, "O salão informado não foi encontrado.", StringComparison.Ordinal))
        {
            return NotFound(new { message = error });
        }

        return BadRequest(new { message = error });
    }
}
