using AlyUp.Application.DTOs.ProfessionalAvailability;
using AlyUp.Application.Security;
using AlyUp.Application.UseCases.ProfessionalAvailability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/professionals/{professionalId:guid}/availability")]
[Authorize]
public class ProfessionalsController : ControllerBase
{
    private readonly CreateProfessionalAvailabilityUseCase _createAvailabilityUseCase;
    private readonly ListProfessionalAvailabilityUseCase _listAvailabilityUseCase;
    private readonly UpdateProfessionalAvailabilityUseCase _updateAvailabilityUseCase;
    private readonly DeleteProfessionalAvailabilityUseCase _deleteAvailabilityUseCase;

    public ProfessionalsController(
        CreateProfessionalAvailabilityUseCase createAvailabilityUseCase,
        ListProfessionalAvailabilityUseCase listAvailabilityUseCase,
        UpdateProfessionalAvailabilityUseCase updateAvailabilityUseCase,
        DeleteProfessionalAvailabilityUseCase deleteAvailabilityUseCase)
    {
        _createAvailabilityUseCase = createAvailabilityUseCase;
        _listAvailabilityUseCase = listAvailabilityUseCase;
        _updateAvailabilityUseCase = updateAvailabilityUseCase;
        _deleteAvailabilityUseCase = deleteAvailabilityUseCase;
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireSalonOwnerOrMaster)]
    public async Task<IActionResult> Create(Guid professionalId, [FromBody] CreateProfessionalAvailabilityRequestDto request)
    {
        var result = await _createAvailabilityUseCase.ExecuteAsync(professionalId, request);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error);
        }

        return CreatedAtAction(nameof(GetAll), new { professionalId }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid professionalId, [FromQuery] bool includeInactive = false)
    {
        var result = await _listAvailabilityUseCase.ExecuteAsync(professionalId, includeInactive);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPut("{availabilityId:guid}")]
    [Authorize(Policy = AppPolicies.RequireSalonOwnerOrMaster)]
    public async Task<IActionResult> Update(
        Guid professionalId,
        Guid availabilityId,
        [FromBody] UpdateProfessionalAvailabilityRequestDto request)
    {
        var result = await _updateAvailabilityUseCase.ExecuteAsync(professionalId, availabilityId, request);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{availabilityId:guid}")]
    [Authorize(Policy = AppPolicies.RequireSalonOwnerOrMaster)]
    public async Task<IActionResult> Delete(Guid professionalId, Guid availabilityId)
    {
        var result = await _deleteAvailabilityUseCase.ExecuteAsync(professionalId, availabilityId);
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

        if (string.Equals(error, "O profissional informado não foi encontrado.", StringComparison.Ordinal) ||
            string.Equals(error, "Disponibilidade não encontrada.", StringComparison.Ordinal))
        {
            return NotFound(new { message = error });
        }

        return BadRequest(new { message = error });
    }
}
