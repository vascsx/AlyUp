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
    private readonly Mock<IInputNormalizer> _inputNormalizerMock = new();
    private readonly LoginUseCase _sut;

    public LoginUseCaseTests()
    {
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(email => email.Trim().ToLowerInvariant());

        _sut = new LoginUseCase(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _inputNormalizerMock.Object);
    }

    [Fact]
    public async Task Should_ReturnToken_When_LoginIsValid()
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
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Name.Should().Be(user.Name);
        result.Value.Role.Should().Be(user.Role);
        result.Value.SalonId.Should().Be(user.SalonId);

        _inputNormalizerMock.Verify(normalizer => normalizer.NormalizeEmail(request.Email), Times.Once);
        _jwtTokenGeneratorMock.Verify(generator => generator.GenerateToken(user), Times.Once);
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
        result.Value.Should().BeNull();

        _jwtTokenGeneratorMock.Verify(generator => generator.GenerateToken(It.IsAny<User>()), Times.Never);
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
        result.Value.Should().BeNull();

        _passwordHasherMock.Verify(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _jwtTokenGeneratorMock.Verify(generator => generator.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Should_ReturnGenericFailure_When_UserIsInactive()
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
        result.Value.Should().BeNull();

        _jwtTokenGeneratorMock.Verify(generator => generator.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
