using AlyUp.Application.Security;
using AlyUp.Application.UseCases.Auth;
using AlyUp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPolicies.RequireClient)]
public class ClientController : ControllerBase
{
    private readonly GetCurrentUserProfileUseCase _getCurrentUserProfileUseCase;

    public ClientController(GetCurrentUserProfileUseCase getCurrentUserProfileUseCase)
    {
        _getCurrentUserProfileUseCase = getCurrentUserProfileUseCase;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await _getCurrentUserProfileUseCase.ExecuteAsync(UserRole.Client);
        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }
}
