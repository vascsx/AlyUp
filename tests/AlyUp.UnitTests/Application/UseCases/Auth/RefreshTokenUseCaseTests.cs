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
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IAccessTokenLifetimeProvider> _accessTokenLifetimeProviderMock = new();
    private readonly Mock<IRefreshTokenLifetimeProvider> _refreshTokenLifetimeProviderMock = new();
    private readonly RefreshTokenUseCase _sut;

    public RefreshTokenUseCaseTests()
    {
        _refreshTokenGeneratorMock.Setup(generator => generator.Generate()).Returns("new-refresh-token");
        _refreshTokenHasherMock.Setup(hasher => hasher.Hash("old-refresh-token")).Returns("old-refresh-token-hash");
        _refreshTokenHasherMock.Setup(hasher => hasher.Hash("new-refresh-token")).Returns("new-refresh-token-hash");
        _jwtTokenGeneratorMock.Setup(generator => generator.GenerateToken(It.IsAny<User>())).Returns("new-access-token");
        _accessTokenLifetimeProviderMock.Setup(provider => provider.GetLifetimeInMinutes()).Returns(30);
        _refreshTokenLifetimeProviderMock.Setup(provider => provider.GetLifetimeInDays()).Returns(30);
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        _sut = new RefreshTokenUseCase(
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _jwtTokenGeneratorMock.Object,
            _refreshTokenGeneratorMock.Object,
            _refreshTokenHasherMock.Object,
            _unitOfWorkMock.Object,
            _accessTokenLifetimeProviderMock.Object,
            _refreshTokenLifetimeProviderMock.Object);
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
            SessionId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            TokenHash = "old-refresh-token-hash",
            Created = DateTime.UtcNow.AddDays(-1),
            Expires = DateTime.UtcNow.AddDays(1)
        };

        _refreshTokenRepositoryMock.Setup(repository => repository.GetByTokenHashAsync("old-refresh-token-hash")).ReturnsAsync(refreshToken);
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.ExecuteAsync(new RefreshTokenRequestDto("old-refresh-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        result.Value.ExpiresInMinutes.Should().Be(30);
        refreshToken.Revoked.Should().NotBeNull();

        _refreshTokenRepositoryMock.Verify(repository => repository.UpdateAsync(refreshToken), Times.Once);
        _refreshTokenRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<RefreshToken>(token =>
            token.UserId == user.Id &&
            token.TokenHash == "new-refresh-token-hash" &&
            token.SessionId == refreshToken.SessionId &&
            token.FamilyId == refreshToken.FamilyId)), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_RefreshTokenIsInvalid()
    {
        _refreshTokenHasherMock.Setup(hasher => hasher.Hash("invalid")).Returns("invalid-hash");
        _refreshTokenRepositoryMock.Setup(repository => repository.GetByTokenHashAsync("invalid-hash")).ReturnsAsync((RefreshToken?)null);

        var result = await _sut.ExecuteAsync(new RefreshTokenRequestDto("invalid"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Refresh token inválido ou expirado.");
    }
}
