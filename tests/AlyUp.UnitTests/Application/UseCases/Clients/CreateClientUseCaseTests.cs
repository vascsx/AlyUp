using AlyUp.Application.DTOs.Client;
using AlyUp.Application.Interfaces;
using AlyUp.Application.UseCases.Clients;
using AlyUp.Domain.Entities;
using FluentAssertions;
using Moq;

namespace AlyUp.UnitTests.Application.UseCases.Clients;

public class CreateClientUseCaseTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock = new();
    private readonly CreateClientUseCase _sut;

    public CreateClientUseCaseTests()
    {
        _sut = new CreateClientUseCase(_clientRepositoryMock.Object);
    }

    [Fact]
    public async Task Should_ReturnId_When_ClientIsCreatedSuccessfully()
    {
        // Arrange
        var request = new CreateClientRequestDto(
            "  Maria Souza  ",
            "  (11) 99999-0000  ",
            "  Maria.Souza@Email.com  ",
            "  Cliente recorrente  ");
        var salonId = Guid.NewGuid();

        // Act
        var result = await _sut.ExecuteAsync(request, salonId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().NotBeEmpty();

        _clientRepositoryMock.Verify(repository => repository.CreateAsync(It.Is<Client>(client =>
            client.SalonId == salonId &&
            client.Name == "Maria Souza" &&
            client.Phone == "(11) 99999-0000" &&
            client.Email == "maria.souza@email.com" &&
            client.Notes == "Cliente recorrente")), Times.Once);
    }
}