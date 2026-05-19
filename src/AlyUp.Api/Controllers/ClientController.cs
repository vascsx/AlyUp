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
            if (IsForbidden(result.Error))
            {
                return Forbid();
            }

            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    private static bool IsForbidden(string? error) =>
        string.Equals(error, "Usuário não autorizado.", StringComparison.Ordinal);
}
