using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Admin;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.Admin;

public class CreateSalonOwnerUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ISalonRepository> _salonRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IInputNormalizer> _inputNormalizerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CreateSalonOwnerUseCase _sut;

    public CreateSalonOwnerUseCaseTests()
    {
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(email => email.Trim().ToLowerInvariant());
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeText(It.IsAny<string>()))
            .Returns<string>(value => value.Trim());
        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        _sut = new CreateSalonOwnerUseCase(
            _userRepositoryMock.Object,
            _salonRepositoryMock.Object,
            _passwordHasherMock.Object,
            _inputNormalizerMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Should_Create_SalonOwner_UsingCurrentValues_InsideTransaction()
    {
        var request = new CreateSalonOwnerRequestDto(
            "  Maria Owner  ",
            "  Maria.Owner@Email.com  ",
            "Password123!",
            "  Salao Central  ",
            "  123456789  ",
            "  Rua A, 100  ");

        _userRepositoryMock
            .Setup(repository => repository.ExistsByEmailAsync("maria.owner@email.com"))
            .ReturnsAsync(false);

        _salonRepositoryMock
            .Setup(repository => repository.ExistsBySalonDocumentAsync("  123456789  "))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(hasher => hasher.Hash("Password123!"))
            .Returns("hashed-password");

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _unitOfWorkMock.Verify(unitOfWork =>
            unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Once);

        _salonRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<AlyUp.Domain.Entities.Salon>(salon =>
            salon.Name == "  Maria Owner  " &&
            salon.Document == "  123456789  " &&
            salon.Address == "  Rua A, 100  ")), Times.Once);

        _userRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<User>(user =>
            user.Name == "  Maria Owner  " &&
            user.Email == "maria.owner@email.com" &&
            user.PasswordHash == "hashed-password" &&
            user.Role == UserRole.SalonOwner &&
            user.SalonId.HasValue)), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_EmailAlreadyExists()
    {
        var request = new CreateSalonOwnerRequestDto(
            "Maria Owner",
            "maria.owner@email.com",
            "Password123!",
            "Salao Central",
            "123456789",
            "Rua A, 100");

        _userRepositoryMock
            .Setup(repository => repository.ExistsByEmailAsync("maria.owner@email.com"))
            .ReturnsAsync(true);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Já existe uma conta cadastrada com este e-mail.");

        _unitOfWorkMock.Verify(unitOfWork =>
            unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_SalonDocumentAlreadyExists()
    {
        var request = new CreateSalonOwnerRequestDto(
            "Maria Owner",
            "maria.owner@email.com",
            "Password123!",
            "Salao Central",
            "123456789",
            "Rua A, 100");

        _userRepositoryMock
            .Setup(repository => repository.ExistsByEmailAsync("maria.owner@email.com"))
            .ReturnsAsync(false);

        _salonRepositoryMock
            .Setup(repository => repository.ExistsBySalonDocumentAsync("123456789"))
            .ReturnsAsync(true);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Já existe um salão cadastrado com este documento.");

        _unitOfWorkMock.Verify(unitOfWork =>
            unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
