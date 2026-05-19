using AlyUp.Application.DTOs.Services;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Services;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.Services;

public class CreateServiceUseCaseTests
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Mock<ISalonRepository> _salonRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IAccessScopeService> _accessScopeServiceMock = new();
    private readonly Mock<IInputNormalizer> _inputNormalizerMock = new();
    private readonly CreateServiceUseCase _sut;

    public CreateServiceUseCaseTests()
    {
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeText(It.IsAny<string>()))
            .Returns<string>(value => value.Trim());
        _inputNormalizerMock
            .Setup(normalizer => normalizer.NormalizeNullableText(It.IsAny<string?>()))
            .Returns<string?>(value => value?.Trim() ?? string.Empty);

        _sut = new CreateServiceUseCase(
            _serviceRepositoryMock.Object,
            _salonRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _accessScopeServiceMock.Object,
            _inputNormalizerMock.Object);
    }

    [Fact]
    public async Task Should_CreateService_When_SalonOwnerUsesOwnSalonScope()
    {
        var salonId = Guid.NewGuid();
        var request = new CreateServiceRequestDto(" Corte ", " Corte masculino ", 45, 59.9m);

        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.Master)).Returns(false);
        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.SalonOwner)).Returns(true);
        _accessScopeServiceMock.Setup(service => service.ResolveSalonScope(null)).Returns(salonId);
        _salonRepositoryMock
            .Setup(repository => repository.GetByIdAsync(salonId))
            .ReturnsAsync(new AlyUp.Domain.Entities.Salon { Id = salonId, Name = "Salão", Document = "123", Address = "Rua 1" });

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        _serviceRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<Service>(service =>
            service.SalonId == salonId &&
            service.Name == "Corte" &&
            service.Description == "Corte masculino" &&
            service.DurationInMinutes == 45 &&
            service.Price == 59.9m &&
            service.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_MasterDoesNotInformSalonId()
    {
        var request = new CreateServiceRequestDto("Corte", null, 45, 59.9m);

        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.Master)).Returns(true);
        _accessScopeServiceMock.Setup(service => service.ResolveSalonScope(null)).Returns((Guid?)null);

        var result = await _sut.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Não foi possível identificar o salão responsável pelo serviço.");

        _serviceRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<Service>()), Times.Never);
    }
}
