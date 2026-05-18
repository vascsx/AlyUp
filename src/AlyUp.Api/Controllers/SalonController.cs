using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.Security;
using AlyUp.Application.UseCases.Salon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPolicies.RequireSalonOwnerOrMaster)]
public class SalonController : ControllerBase
{
    private readonly CreateProfessionalUseCase _createProfessionalUseCase;
    private readonly IAccessScopeService _accessScopeService;

    public SalonController(
        CreateProfessionalUseCase createProfessionalUseCase,
        IAccessScopeService accessScopeService)
    {
        _createProfessionalUseCase = createProfessionalUseCase;
        _accessScopeService = accessScopeService;
    }

    [HttpPost("createProfessionals")]
    public async Task<IActionResult> CreateProfessional([FromBody] CreateProfessionalRequestDto request)
    {
        var salonId = _accessScopeService.ResolveSalonScope(request.SalonId);

        if (!salonId.HasValue || salonId.Value == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Não foi possível identificar o salão responsável pelo cadastro do profissional."
            });
        }

        var result = await _createProfessionalUseCase.ExecuteAsync(request, salonId.Value);

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Profissional cadastrado com sucesso."
        });
    }
}