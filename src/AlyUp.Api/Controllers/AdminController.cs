using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.UseCases.Admin;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly CreateSalonOwnerUseCase _createSalonOwnerUseCase;

    public AdminController(CreateSalonOwnerUseCase createSalonOwnerUseCase)
    {
        _createSalonOwnerUseCase = createSalonOwnerUseCase;
    }

    [HttpPost("registerSalonOwner")]
    public async Task<IActionResult> CreateSalonOwner(
        [FromBody] CreateSalonOwnerRequestDto request)
    {

        var result = await _createSalonOwnerUseCase.ExecuteAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Created("", new
        {
            message = "Dono de salão e salão criados com sucesso.",
            id = result.Value
        });
    }
}