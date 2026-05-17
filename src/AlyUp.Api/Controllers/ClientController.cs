using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlyUp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPolicies.RequireClient)]
public class ClientController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAccessScopeService _accessScopeService;
    private readonly IUserRepository _userRepository;

    public ClientController(
        ICurrentUserService currentUserService,
        IAccessScopeService accessScopeService,
        IUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _accessScopeService = accessScopeService;
        _userRepository = userRepository;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (!_currentUserService.UserId.HasValue || !_accessScopeService.CanAccessUser(_currentUserService.UserId.Value))
            return Unauthorized(new { message = "Usuario nao autenticado." });

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value);
        if (user is null)
            return NotFound(new { message = "Usuario nao encontrado." });

        return Ok(new UserResponseDto(user.Id, user.Name, user.Email, user.Role, user.SalonId));
    }
}
