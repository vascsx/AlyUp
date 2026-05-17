using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
    {
        await _logoutUseCase.ExecuteAsync(request);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("registerClient")]
    public async Task<IActionResult> RegisterClient([FromBody] RegisterClientRequestDto request)
    {
        var result = await _registerClientUseCase.ExecuteAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Created(string.Empty, new
        {
            message = "Cliente registrado com sucesso.",
            id = result.Value
        });
    }
}
