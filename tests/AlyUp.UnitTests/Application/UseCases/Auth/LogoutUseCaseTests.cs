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
    private readonly LogoutUseCase _sut;

    public LogoutUseCaseTests()
    {
        _sut = new LogoutUseCase(_refreshTokenRepositoryMock.Object);
    }

    [Fact]
    public async Task Should_RevokeRefreshToken_When_ActiveTokenExists()
    {
        var refreshToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "refresh-token",
            Created = DateTime.UtcNow.AddDays(-1),
            Expires = DateTime.UtcNow.AddDays(1)
        };

        _refreshTokenRepositoryMock
            .Setup(repository => repository.GetByTokenAsync("refresh-token"))
            .ReturnsAsync(refreshToken);

        var result = await _sut.ExecuteAsync(new LogoutRequestDto("refresh-token"));

        result.IsSuccess.Should().BeTrue();
        refreshToken.Revoked.Should().NotBeNull();
        _refreshTokenRepositoryMock.Verify(repository => repository.UpdateAsync(refreshToken), Times.Once);
    }
}
