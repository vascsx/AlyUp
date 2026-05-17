using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Security;
using AlyUp.Application.UseCases.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPolicies.RequireMaster)]
public class AdminController : ControllerBase
{
    private readonly CreateSalonOwnerUseCase _createSalonOwnerUseCase;

    public AdminController(CreateSalonOwnerUseCase createSalonOwnerUseCase)
    {
        _createSalonOwnerUseCase = createSalonOwnerUseCase;
    }

    [HttpPost("registerSalonOwner")]
    public async Task<IActionResult> CreateSalonOwner([FromBody] CreateSalonOwnerRequestDto request)
    {
        var result = await _createSalonOwnerUseCase.ExecuteAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Created(string.Empty, new
        {
            message = "Dono de salao e salao criados com sucesso.",
            id = result.Value
        });
    }
}
