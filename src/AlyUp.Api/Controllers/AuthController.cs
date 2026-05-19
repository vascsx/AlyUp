using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Security;
using AlyUp.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly RefreshTokenUseCase _refreshTokenUseCase;
    private readonly LogoutUseCase _logoutUseCase;
    private readonly RegisterClientUseCase _registerClientUseCase;

    public AuthController(
        LoginUseCase loginUseCase,
        RefreshTokenUseCase refreshTokenUseCase,
        LogoutUseCase logoutUseCase,
        RegisterClientUseCase registerClientUseCase)
    {
        _loginUseCase = loginUseCase;
        _refreshTokenUseCase = refreshTokenUseCase;
        _logoutUseCase = logoutUseCase;
        _registerClientUseCase = registerClientUseCase;
    }

    [AllowAnonymous]
    [EnableRateLimiting(AppRateLimitPolicies.AuthLogin)]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _loginUseCase.ExecuteAsync(request);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [AllowAnonymous]
    [EnableRateLimiting(AppRateLimitPolicies.AuthRefresh)]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _refreshTokenUseCase.ExecuteAsync(request);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [Authorize]
    [EnableRateLimiting(AppRateLimitPolicies.AuthLogout)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
    {
        await _logoutUseCase.ExecuteAsync(request);
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting(AppRateLimitPolicies.AuthRegisterClient)]
    [HttpPost("registerClient")]
    public async Task<IActionResult> RegisterClient([FromBody] RegisterClientRequestDto request)
    {
        var result = await _registerClientUseCase.ExecuteAsync(request);

        if (!result.IsSuccess)
        {
            if (IsConflict(result.Error))
            {
                return Conflict(new { message = result.Error });
            }

            return BadRequest(new { message = result.Error });
        }

        return Created(string.Empty, new
        {
            message = "Cliente registrado com sucesso.",
            id = result.Value
        });
    }

    private static bool IsConflict(string? error) =>
        !string.IsNullOrWhiteSpace(error) && error.Contains("Já existe", StringComparison.OrdinalIgnoreCase);
}
