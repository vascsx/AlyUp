using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Auth;
using AlyUp.Domain.Entities;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.Auth;

public class LogoutUseCaseTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly LogoutUseCase _sut;

    public LogoutUseCaseTests()
    {
        _currentUserServiceMock.Setup(service => service.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(service => service.UserId).Returns(Guid.NewGuid());

        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        _refreshTokenHasherMock.Setup(hasher => hasher.Hash("refresh-token")).Returns("refresh-token-hash");

        _sut = new LogoutUseCase(
            _refreshTokenRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _refreshTokenHasherMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Should_RevokeRefreshToken_When_ActiveTokenExists()
    {
        var userId = _currentUserServiceMock.Object.UserId!.Value;
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            SessionId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            TokenHash = "refresh-token-hash",
            Created = DateTime.UtcNow.AddDays(-1),
            Expires = DateTime.UtcNow.AddDays(1)
        };
        var user = new User
        {
            Id = userId,
            Name = "Client",
            Email = "client@email.com",
            PasswordHash = "hash"
        };

        _refreshTokenRepositoryMock
            .Setup(repository => repository.GetByTokenHashAsync("refresh-token-hash"))
            .ReturnsAsync(refreshToken);

        var result = await _sut.ExecuteAsync(new LogoutRequestDto("refresh-token"));

        result.IsSuccess.Should().BeTrue();
        refreshToken.Revoked.Should().NotBeNull();
        _refreshTokenRepositoryMock.Verify(repository => repository.UpdateAsync(refreshToken), Times.Once);
    }

    [Fact]
    public async Task Should_Fail_When_UserIsNotAuthenticated()
    {
        _currentUserServiceMock.Setup(service => service.IsAuthenticated).Returns(false);
        _currentUserServiceMock.Setup(service => service.UserId).Returns((Guid?)null);

        var result = await _sut.ExecuteAsync(new LogoutRequestDto("refresh-token"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Usuário não autenticado.");
        _refreshTokenRepositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<RefreshToken>()), Times.Never);
    }
}
