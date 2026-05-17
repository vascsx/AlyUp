using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Salon;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.Salon;

public class CreateProfessionalUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly CreateProfessionalUseCase _sut;

    public CreateProfessionalUseCaseTests()
    {
        _sut = new CreateProfessionalUseCase(_userRepositoryMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Should_ReturnId_When_ProfessionalIsCreatedSuccessfully()
    {
        // Arrange
        var request = new CreateProfessionalRequestDto("Ana Silva", "ana.silva@email.com", "password123");
        var salonId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(repository => repository.ExistsByEmailAsync("ana.silva@email.com"))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(hasher => hasher.Hash("password123"))
            .Returns("hashed-password");

        // Act
        var result = await _sut.ExecuteAsync(request, salonId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().NotBeEmpty();

        _userRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<User>(user =>
            user.Name == "Ana Silva" &&
            user.Email == "ana.silva@email.com" &&
            user.PasswordHash == "hashed-password" &&
            user.Role == UserRole.Professional &&
            user.SalonId == salonId &&
            user.IsActive)), Times.Once);

        _passwordHasherMock.Verify(hasher => hasher.Hash("password123"), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_EmailAlreadyExists()
    {
        // Arrange
        var request = new CreateProfessionalRequestDto("Ana Silva", "ana.silva@email.com", "password123");
        var salonId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(repository => repository.ExistsByEmailAsync("ana.silva@email.com"))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ExecuteAsync(request, salonId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email já cadastrado.");
        result.Value.Should().Be(default(Guid));

        _passwordHasherMock.Verify(hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
        _userRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<User>()), Times.Never);
    }
}