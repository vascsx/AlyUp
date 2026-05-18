using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Auth;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.Auth;

public class RegisterClientUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IInputNormalizer> _inputNormalizerMock = new();
    private readonly RegisterClientUseCase _sut;

    public RegisterClientUseCaseTests()
    {
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(email => email.Trim().ToLowerInvariant());
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeText(It.IsAny<string>()))
            .Returns<string>(value => value.Trim());

        _sut = new RegisterClientUseCase(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _inputNormalizerMock.Object);
    }

    [Fact]
    public async Task Should_ReturnId_When_ClientIsCreatedSuccessfully()
    {
        var request = new RegisterClientRequestDto("  John Doe  ", "  John.Doe@Email.com  ", "password123");

        _userRepositoryMock
            .Setup(repository => repository.ExistsByEmailAsync("john.doe@email.com"))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(hasher => hasher.Hash("password123"))
            .Returns("hashed-password");

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _userRepositoryMock.Verify(repository => repository.ExistsByEmailAsync("john.doe@email.com"), Times.Once);
        _userRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<User>(user =>
            user.Name == "  John Doe  " &&
            user.Email == "john.doe@email.com" &&
            user.PasswordHash == "hashed-password" &&
            user.Role == UserRole.Client &&
            user.SalonId == null &&
            user.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_EmailAlreadyExists()
    {
        var request = new RegisterClientRequestDto("John Doe", "  John.Doe@Email.com  ", "password123");

        _userRepositoryMock
            .Setup(repository => repository.ExistsByEmailAsync("john.doe@email.com"))
            .ReturnsAsync(true);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Já existe uma conta de cliente cadastrada com este e-mail.");
        result.Value.Should().Be(default(Guid));

        _passwordHasherMock.Verify(hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
        _userRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<User>()), Times.Never);
    }
}
