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
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly LogoutUseCase _sut;

    public LogoutUseCaseTests()
    {
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        _sut = new LogoutUseCase(
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Should_RevokeRefreshToken_When_ActiveTokenExists()
    {
        var userId = Guid.NewGuid();
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = "refresh-token",
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
            .Setup(repository => repository.GetByTokenAsync("refresh-token"))
            .ReturnsAsync(refreshToken);
        _userRepositoryMock
            .Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync(user);

        var result = await _sut.ExecuteAsync(new LogoutRequestDto("refresh-token"));

        result.IsSuccess.Should().BeTrue();
        refreshToken.Revoked.Should().NotBeNull();
        user.UpdatedAt.Should().Be(refreshToken.Revoked);
        _refreshTokenRepositoryMock.Verify(repository => repository.UpdateAsync(refreshToken), Times.Once);
        _userRepositoryMock.Verify(repository => repository.UpdateAsync(user), Times.Once);
    }
}
