using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.UseCases.Salon;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SalonOwner")]
public class SalonController : ControllerBase
{
    private readonly CreateProfessionalUseCase _createProfessionalUseCase;

    public SalonController(CreateProfessionalUseCase createProfessionalUseCase)
    {
        _createProfessionalUseCase = createProfessionalUseCase;
    }

    [HttpPost("createProfessionals")]
    public async Task<IActionResult> CreateProfessional(CreateProfessionalRequestDto request)
    {
        var salonIdClaim = User.FindFirst("SalonId")?.Value;
        if (string.IsNullOrEmpty(salonIdClaim))
        {
            return BadRequest(new { message = "Salão não identificado no token." });
        }

        var salonId = Guid.Parse(salonIdClaim);

        var result = await _createProfessionalUseCase.ExecuteAsync(request, salonId);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "Profissional criado com sucesso." });
    }
}
