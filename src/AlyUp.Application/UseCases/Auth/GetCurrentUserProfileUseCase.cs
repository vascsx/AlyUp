using AlyUp.Application.Common;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Domain.Enums;

namespace AlyUp.Application.UseCases.Auth;

public class GetCurrentUserProfileUseCase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;

    public GetCurrentUserProfileUseCase(
        ICurrentUserService currentUserService,
        IUserRepository userRepository)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    public async Task<Result<UserResponseDto>> ExecuteAsync(UserRole expectedRole)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Result<UserResponseDto>.Failure("Usuario nao autenticado.");
        }

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value);
        if (user is null || !user.IsActive)
        {
            return Result<UserResponseDto>.Failure("Usuario nao autenticado.");
        }

        if (user.Role != expectedRole || _currentUserService.Role != user.Role)
        {
            return Result<UserResponseDto>.Failure("Usuario nao autorizado.");
        }

        if (user.SalonId != _currentUserService.SalonId)
        {
            return Result<UserResponseDto>.Failure("Usuario nao autorizado.");
        }

        return Result<UserResponseDto>.Success(new UserResponseDto(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.SalonId));
    }
}
