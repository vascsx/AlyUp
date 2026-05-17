using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Auth;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.Auth;

public class LoginUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IInputNormalizer> _inputNormalizerMock = new();
    private readonly LoginUseCase _sut;

    public LoginUseCaseTests()
    {
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(email => email.Trim().ToLowerInvariant());

        _refreshTokenGeneratorMock
            .Setup(generator => generator.Generate())
            .Returns("refresh-token");

        _sut = new LoginUseCase(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _refreshTokenGeneratorMock.Object,
            _refreshTokenRepositoryMock.Object,
            _inputNormalizerMock.Object);
    }

    [Fact]
    public async Task Should_ReturnAccessAndRefreshTokens_When_LoginIsValid()
    {
        var request = new LoginRequestDto("  John.Doe@Email.com ", "password123");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john.doe@email.com",
            PasswordHash = "hashed-password",
            Role = UserRole.Client,
            SalonId = Guid.NewGuid(),
            IsActive = true
        };

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync("john.doe@email.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(hasher => hasher.Verify("password123", user.PasswordHash))
            .Returns(true);

        _jwtTokenGeneratorMock
            .Setup(generator => generator.GenerateToken(user))
            .Returns("jwt-token");

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("jwt-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Role.Should().Be(user.Role);

        _refreshTokenRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<RefreshToken>(token =>
            token.UserId == user.Id &&
            token.Token == "refresh-token" &&
            token.Revoked == null)), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_PasswordIsInvalid()
    {
        var request = new LoginRequestDto("john.doe@email.com", "invalid-password");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john.doe@email.com",
            PasswordHash = "hashed-password",
            Role = UserRole.Client,
            IsActive = true
        };

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync("john.doe@email.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(hasher => hasher.Verify("invalid-password", user.PasswordHash))
            .Returns(false);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email ou senha invalidos.");
        _refreshTokenRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_UserDoesNotExist()
    {
        var request = new LoginRequestDto("missing@email.com", "password123");

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync("missing@email.com"))
            .ReturnsAsync((User?)null);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email ou senha invalidos.");
        _refreshTokenRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_UserIsInactive()
    {
        var request = new LoginRequestDto("john.doe@email.com", "password123");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john.doe@email.com",
            PasswordHash = "hashed-password",
            Role = UserRole.Client,
            IsActive = false
        };

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync("john.doe@email.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(hasher => hasher.Verify("password123", user.PasswordHash))
            .Returns(true);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email ou senha invalidos.");
        _refreshTokenRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<RefreshToken>()), Times.Never);
    }
}
