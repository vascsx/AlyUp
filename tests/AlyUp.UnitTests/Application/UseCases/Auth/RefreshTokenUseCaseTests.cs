using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Auth;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.Auth;

public class RefreshTokenUseCaseTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RefreshTokenUseCase _sut;

    public RefreshTokenUseCaseTests()
    {
        _refreshTokenGeneratorMock.Setup(generator => generator.Generate()).Returns("new-refresh-token");
        _jwtTokenGeneratorMock.Setup(generator => generator.GenerateToken(It.IsAny<User>())).Returns("new-access-token");
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        _sut = new RefreshTokenUseCase(
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _jwtTokenGeneratorMock.Object,
            _refreshTokenGeneratorMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Should_RotateRefreshToken_When_RequestIsValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Refresh User",
            Email = "refresh@email.com",
            PasswordHash = "hash",
            Role = UserRole.Client,
            IsActive = true
        };
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "old-refresh-token",
            Created = DateTime.UtcNow.AddDays(-1),
            Expires = DateTime.UtcNow.AddDays(1)
        };

        _refreshTokenRepositoryMock.Setup(repository => repository.GetByTokenAsync("old-refresh-token")).ReturnsAsync(refreshToken);
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.ExecuteAsync(new RefreshTokenRequestDto("old-refresh-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        refreshToken.Revoked.Should().NotBeNull();

        _refreshTokenRepositoryMock.Verify(repository => repository.UpdateAsync(refreshToken), Times.Once);
        _refreshTokenRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<RefreshToken>(token =>
            token.UserId == user.Id &&
            token.Token == "new-refresh-token")), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_RefreshTokenIsInvalid()
    {
        _refreshTokenRepositoryMock.Setup(repository => repository.GetByTokenAsync("invalid")).ReturnsAsync((RefreshToken?)null);

        var result = await _sut.ExecuteAsync(new RefreshTokenRequestDto("invalid"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Refresh token invalido ou expirado.");
    }
}
