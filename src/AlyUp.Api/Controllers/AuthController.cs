using Microsoft.AspNetCore.Mvc;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.UseCases.Auth;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly RegisterClientUseCase _registerClientUseCase;

    public AuthController(
        LoginUseCase loginUseCase,
        RegisterClientUseCase registerClientUseCase)
    {
        _loginUseCase = loginUseCase;
        _registerClientUseCase = registerClientUseCase;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _loginUseCase.ExecuteAsync(request);

        if (!result.IsSuccess)
        {
            return result.Error == "Usuário inativo."
                ? StatusCode(403, new { message = result.Error })
                : Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("registerClient")]
    public async Task<IActionResult> RegisterClient([FromBody] RegisterClientRequestDto request)
    {
        var result = await _registerClientUseCase.ExecuteAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Created("", new
        {
            message = "Cliente registrado com sucesso.",
            id = result.Value
        });
    }
}