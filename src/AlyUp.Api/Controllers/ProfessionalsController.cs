using AlyUp.Application.DTOs.ProfessionalAvailability;
using AlyUp.Application.Security;
using AlyUp.Application.UseCases.Auth;
using AlyUp.Application.UseCases.ProfessionalAvailability;
using AlyUp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/professionals")]
[Authorize]
public class ProfessionalsController : ControllerBase
{
    private const string UnauthorizedMessage = "Usuário não autorizado.";
    private const string ProfessionalNotFoundMessage = "O profissional informado não foi encontrado.";
    private const string AvailabilityNotFoundMessage = "Disponibilidade não encontrada.";

    private readonly CreateProfessionalAvailabilityUseCase _createAvailabilityUseCase;
    private readonly ListProfessionalAvailabilityUseCase _listAvailabilityUseCase;
    private readonly UpdateProfessionalAvailabilityUseCase _updateAvailabilityUseCase;
    private readonly DeleteProfessionalAvailabilityUseCase _deleteAvailabilityUseCase;
    private readonly GetCurrentUserProfileUseCase _getCurrentUserProfileUseCase;

    public ProfessionalsController(
        CreateProfessionalAvailabilityUseCase createAvailabilityUseCase,
        ListProfessionalAvailabilityUseCase listAvailabilityUseCase,
        UpdateProfessionalAvailabilityUseCase updateAvailabilityUseCase,
        DeleteProfessionalAvailabilityUseCase deleteAvailabilityUseCase,
        GetCurrentUserProfileUseCase getCurrentUserProfileUseCase)
    {
        _createAvailabilityUseCase = createAvailabilityUseCase;
        _listAvailabilityUseCase = listAvailabilityUseCase;
        _updateAvailabilityUseCase = updateAvailabilityUseCase;
        _deleteAvailabilityUseCase = deleteAvailabilityUseCase;
        _getCurrentUserProfileUseCase = getCurrentUserProfileUseCase;
    }

    [HttpPost("{professionalId:guid}/availability")]
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

    [HttpGet("{professionalId:guid}/availability")]
    public async Task<IActionResult> GetAll(Guid professionalId, [FromQuery] bool includeInactive = false)
    {
        var result = await _listAvailabilityUseCase.ExecuteAsync(professionalId, includeInactive);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPut("{professionalId:guid}/availability/{availabilityId:guid}")]
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

    [HttpDelete("{professionalId:guid}/availability/{availabilityId:guid}")]
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

    [HttpGet("me")]
    [Authorize(Policy = AppPolicies.RequireProfessional)]
    public async Task<IActionResult> Me()
    {
        var result = await _getCurrentUserProfileUseCase.ExecuteAsync(UserRole.Professional);
        if (!result.IsSuccess)
        {
            if (string.Equals(result.Error, UnauthorizedMessage, StringComparison.Ordinal))
            {
                return Forbid();
            }

            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    private IActionResult MapFailure(string? error)
    {
        if (string.Equals(error, UnauthorizedMessage, StringComparison.Ordinal))
        {
            return Forbid();
        }

        if (string.Equals(error, ProfessionalNotFoundMessage, StringComparison.Ordinal) ||
            string.Equals(error, AvailabilityNotFoundMessage, StringComparison.Ordinal))
        {
            return NotFound(new { message = error });
        }

        return BadRequest(new { message = error });
    }
}
