using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.Security;
using AlyUp.Application.UseCases.Salon;
using AlyUp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPolicies.RequireSalonOwnerOrMaster)]
public class SalonController : ControllerBase
{
    private readonly CreateProfessionalUseCase _createProfessionalUseCase;
    private readonly ICurrentUserService _currentUserService;

    public SalonController(
        CreateProfessionalUseCase createProfessionalUseCase,
        ICurrentUserService currentUserService)
    {
        _createProfessionalUseCase = createProfessionalUseCase;
        _currentUserService = currentUserService;
    }

    [HttpPost("createProfessionals")]
    public async Task<IActionResult> CreateProfessional([FromBody] CreateProfessionalRequestDto request)
    {
        Guid? salonId = null;

        if (_currentUserService.IsInRole(UserRole.SalonOwner))
        {
            salonId = _currentUserService.SalonId;
        }
        else if (_currentUserService.IsMaster)
        {
            salonId = request.SalonId;
        }

        if (!salonId.HasValue || salonId.Value == Guid.Empty)
        {
            return BadRequest(new { message = "Salao nao identificado para criacao do profissional." });
        }

        var result = await _createProfessionalUseCase.ExecuteAsync(request, salonId.Value);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "Profissional criado com sucesso." });
    }
}
