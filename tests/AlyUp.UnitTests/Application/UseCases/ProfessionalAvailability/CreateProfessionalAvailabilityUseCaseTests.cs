using AlyUp.Application.DTOs.ProfessionalAvailability;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.ProfessionalAvailability;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.ProfessionalAvailability;

public class CreateProfessionalAvailabilityUseCaseTests
{
    private readonly Mock<IProfessionalAvailabilityRepository> _availabilityRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly CreateProfessionalAvailabilityUseCase _sut;

    public CreateProfessionalAvailabilityUseCaseTests()
    {
        _sut = new CreateProfessionalAvailabilityUseCase(
            _availabilityRepositoryMock.Object,
            _userRepositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Should_CreateAvailability_When_ProfessionalBelongsToOwnersSalon()
    {
        var salonId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var request = new CreateProfessionalAvailabilityRequestDto(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));

        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.Master)).Returns(false);
        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.SalonOwner)).Returns(true);
        _currentUserServiceMock.SetupGet(service => service.SalonId).Returns(salonId);
        _userRepositoryMock
            .Setup(repository => repository.GetByIdAsync(professionalId))
            .ReturnsAsync(new User
            {
                Id = professionalId,
                Role = UserRole.Professional,
                SalonId = salonId
            });
        _availabilityRepositoryMock
            .Setup(repository => repository.ExistsExactAsync(professionalId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0), null, false))
            .ReturnsAsync(false);
        _availabilityRepositoryMock
            .Setup(repository => repository.HasOverlapAsync(professionalId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0), null, false))
            .ReturnsAsync(false);

        var result = await _sut.ExecuteAsync(professionalId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _availabilityRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<AlyUp.Domain.Entities.ProfessionalAvailability>(availability =>
            availability.ProfessionalId == professionalId &&
            availability.SalonId == salonId &&
            availability.DayOfWeek == DayOfWeek.Monday &&
            availability.StartTime == new TimeOnly(9, 0) &&
            availability.EndTime == new TimeOnly(12, 0) &&
            availability.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_ExactAvailabilityAlreadyExists()
    {
        var salonId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var request = new CreateProfessionalAvailabilityRequestDto(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));

        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.Master)).Returns(true);
        _userRepositoryMock
            .Setup(repository => repository.GetByIdAsync(professionalId))
            .ReturnsAsync(new User
            {
                Id = professionalId,
                Role = UserRole.Professional,
                SalonId = salonId
            });
        _availabilityRepositoryMock
            .Setup(repository => repository.ExistsExactAsync(professionalId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0), null, false))
            .ReturnsAsync(true);

        var result = await _sut.ExecuteAsync(professionalId, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Já existe uma disponibilidade cadastrada para o mesmo horário.");
        _availabilityRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<AlyUp.Domain.Entities.ProfessionalAvailability>()), Times.Never);
    }
}
