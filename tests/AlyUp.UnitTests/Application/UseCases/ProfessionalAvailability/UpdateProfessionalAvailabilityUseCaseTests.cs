using AlyUp.Application.DTOs.ProfessionalAvailability;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.ProfessionalAvailability;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.ProfessionalAvailability;

public class UpdateProfessionalAvailabilityUseCaseTests
{
    private readonly Mock<IProfessionalAvailabilityRepository> _availabilityRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly UpdateProfessionalAvailabilityUseCase _sut;

    public UpdateProfessionalAvailabilityUseCaseTests()
    {
        _sut = new UpdateProfessionalAvailabilityUseCase(
            _availabilityRepositoryMock.Object,
            _userRepositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_ProfessionalDoesNotBelongToOwnersSalon()
    {
        var ownerSalonId = Guid.NewGuid();
        var professionalSalonId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var availabilityId = Guid.NewGuid();
        var request = new UpdateProfessionalAvailabilityRequestDto(DayOfWeek.Friday, new TimeOnly(13, 0), new TimeOnly(18, 0));

        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.Master)).Returns(false);
        _currentUserServiceMock.Setup(service => service.IsInRole(UserRole.SalonOwner)).Returns(true);
        _currentUserServiceMock.SetupGet(service => service.SalonId).Returns(ownerSalonId);
        _userRepositoryMock
            .Setup(repository => repository.GetByIdAsync(professionalId))
            .ReturnsAsync(new User
            {
                Id = professionalId,
                Role = UserRole.Professional,
                SalonId = professionalSalonId
            });

        var result = await _sut.ExecuteAsync(professionalId, availabilityId, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("O profissional informado não pertence ao salão do usuário autenticado.");
    }

    [Fact]
    public async Task Should_ReturnFailure_When_UpdateCreatesOverlap()
    {
        var salonId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var availabilityId = Guid.NewGuid();
        var request = new UpdateProfessionalAvailabilityRequestDto(DayOfWeek.Friday, new TimeOnly(13, 0), new TimeOnly(18, 0));

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
            .Setup(repository => repository.GetByIdAsync(availabilityId, false))
            .ReturnsAsync(new AlyUp.Domain.Entities.ProfessionalAvailability
            {
                Id = availabilityId,
                ProfessionalId = professionalId,
                SalonId = salonId,
                DayOfWeek = DayOfWeek.Friday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        _availabilityRepositoryMock
            .Setup(repository => repository.ExistsExactAsync(professionalId, DayOfWeek.Friday, new TimeOnly(13, 0), new TimeOnly(18, 0), availabilityId, false))
            .ReturnsAsync(false);
        _availabilityRepositoryMock
            .Setup(repository => repository.HasOverlapAsync(professionalId, DayOfWeek.Friday, new TimeOnly(13, 0), new TimeOnly(18, 0), availabilityId, false))
            .ReturnsAsync(true);

        var result = await _sut.ExecuteAsync(professionalId, availabilityId, request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Já existe uma disponibilidade com conflito de horário para este profissional.");
        _availabilityRepositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<AlyUp.Domain.Entities.ProfessionalAvailability>()), Times.Never);
    }
}
