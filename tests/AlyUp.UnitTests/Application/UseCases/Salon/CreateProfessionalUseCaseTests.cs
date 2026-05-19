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
    private readonly Mock<ISalonRepository> _salonRepositoryMock = new();
    private readonly Mock<IInputNormalizer> _inputNormalizerMock = new();
    private readonly CreateProfessionalUseCase _sut;

    public CreateProfessionalUseCaseTests()
    {
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(email => email.Trim().ToLowerInvariant());
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeText(It.IsAny<string>()))
            .Returns<string>(value => value.Trim());

        _sut = new CreateProfessionalUseCase(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _salonRepositoryMock.Object,
            _inputNormalizerMock.Object);
    }

    [Fact]
    public async Task Should_ReturnId_When_ProfessionalIsCreatedSuccessfully()
    {
        var request = new CreateProfessionalRequestDto("  Ana Silva  ", "  Ana.Silva@Email.com  ", "password123");
        var salonId = Guid.NewGuid();

        _salonRepositoryMock
            .Setup(repository => repository.GetByIdAsync(salonId))
            .ReturnsAsync(new AlyUp.Domain.Entities.Salon { Id = salonId, Name = "Salao", Document = "123", Address = "Rua 1" });

        _userRepositoryMock
            .Setup(repository => repository.ExistsByEmailAsync("ana.silva@email.com"))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(hasher => hasher.Hash("password123"))
            .Returns("hashed-password");

        var result = await _sut.ExecuteAsync(request, salonId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _userRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<User>(user =>
            user.Name == "  Ana Silva  " &&
            user.Email == "ana.silva@email.com" &&
            user.PasswordHash == "hashed-password" &&
            user.Role == UserRole.Professional &&
            user.SalonId == salonId &&
            user.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_SalonDoesNotExist()
    {
        var request = new CreateProfessionalRequestDto("Ana Silva", "ana.silva@email.com", "password123");
        var salonId = Guid.NewGuid();

        _salonRepositoryMock
            .Setup(repository => repository.GetByIdAsync(salonId))
            .ReturnsAsync((AlyUp.Domain.Entities.Salon?)null);

        var result = await _sut.ExecuteAsync(request, salonId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("O salão informado não foi encontrado.");

        _userRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_EmailAlreadyExists()
    {
        var request = new CreateProfessionalRequestDto("Ana Silva", "ana.silva@email.com", "password123");
        var salonId = Guid.NewGuid();

        _salonRepositoryMock
            .Setup(repository => repository.GetByIdAsync(salonId))
            .ReturnsAsync(new AlyUp.Domain.Entities.Salon { Id = salonId, Name = "Salao", Document = "123", Address = "Rua 1" });

        _userRepositoryMock
            .Setup(repository => repository.ExistsByEmailAsync("ana.silva@email.com"))
            .ReturnsAsync(true);

        var result = await _sut.ExecuteAsync(request, salonId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Já existe um profissional cadastrado com este e-mail.");

        _passwordHasherMock.Verify(hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
        _userRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<User>()), Times.Never);
    }
}
