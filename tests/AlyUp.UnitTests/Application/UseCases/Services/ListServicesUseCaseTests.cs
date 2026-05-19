using AlyUp.Application.DTOs.Services;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Services;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.Services;

public class ListServicesUseCaseTests
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Mock<ISalonRepository> _salonRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IAccessScopeService> _accessScopeServiceMock = new();
    private readonly ListServicesUseCase _sut;

    public ListServicesUseCaseTests()
    {
        _sut = new ListServicesUseCase(
            _serviceRepositoryMock.Object,
            _salonRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _accessScopeServiceMock.Object);
    }

    [Fact]
    public async Task Should_RequireSalonId_When_UserIsClient()
    {
        _currentUserServiceMock.SetupGet(service => service.Role).Returns(UserRole.Client);
        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.Client)).Returns(true);

        var result = await _sut.ExecuteAsync(null, includeInactive: true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("O salão deve ser informado para listar serviços.");
    }

    [Fact]
    public async Task Should_ListOnlyActiveServices_ForClientUsingIgnoreQueryFilters()
    {
        var salonId = Guid.NewGuid();

        _currentUserServiceMock.SetupGet(service => service.Role).Returns(UserRole.Client);
        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.Client)).Returns(true);
        _salonRepositoryMock
            .Setup(repository => repository.GetByIdAsync(salonId))
            .ReturnsAsync(new AlyUp.Domain.Entities.Salon { Id = salonId, Name = "Salão", Document = "123", Address = "Rua 1" });
        _serviceRepositoryMock
            .Setup(repository => repository.GetBySalonIdAsync(salonId, false, true))
            .ReturnsAsync(new[]
            {
                new Service
                {
                    Id = Guid.NewGuid(),
                    SalonId = salonId,
                    Name = "Corte",
                    Description = "Descrição",
                    DurationInMinutes = 30,
                    Price = 40,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            });

        var result = await _sut.ExecuteAsync(salonId, includeInactive: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _serviceRepositoryMock.Verify(repository => repository.GetBySalonIdAsync(salonId, false, true), Times.Once);
    }
}
