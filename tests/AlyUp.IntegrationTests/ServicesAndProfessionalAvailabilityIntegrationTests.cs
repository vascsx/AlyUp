using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlyUp.Application.DTOs.ProfessionalAvailability;
using AlyUp.Application.DTOs.Services;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using AlyUp.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AlyUp.IntegrationTests;

public class ServicesAndProfessionalAvailabilityIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ServicesAndProfessionalAvailabilityIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Master_Should_CreateService_ForRequestedSalon()
    {
        await _factory.ResetDatabaseAsync();

        var masterUserId = Guid.NewGuid();
        var salonId = Guid.NewGuid();
        await _factory.SeedAsync(
            new Salon
            {
                Id = salonId,
                Name = "Salon Master",
                Document = "52998224725",
                Address = "Rua 1"
            },
            new User
            {
                Id = masterUserId,
                Name = "Master",
                Email = "master.services@email.com",
                PasswordHash = "hash",
                Role = UserRole.Master,
                IsActive = true
            });

        var token = _factory.CreateToken(masterUserId, UserRole.Master.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/Services", new
        {
            name = " Corte Premium ",
            description = " Corte masculino premium ",
            durationInMinutes = 45,
            price = 89.9m,
            salonId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<ServiceResponseDto>();
        payload.Should().NotBeNull();
        payload!.SalonId.Should().Be(salonId);
        payload.Name.Should().Be("Corte Premium");
        payload.Description.Should().Be("Corte masculino premium");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Services.Should().Contain(service =>
            service.SalonId == salonId &&
            service.Name == "Corte Premium" &&
            service.IsActive);
    }

    [Fact]
    public async Task SalonOwner_Should_IgnoreRequestedSalonId_And_UseOwnSalon()
    {
        await _factory.ResetDatabaseAsync();

        var ownerSalonId = Guid.NewGuid();
        var otherSalonId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        await _factory.SeedAsync(
            new Salon { Id = ownerSalonId, Name = "Owner Salon", Document = "52998224725", Address = "Rua 1" },
            new Salon { Id = otherSalonId, Name = "Other Salon", Document = "12345678909", Address = "Rua 2" },
            new User
            {
                Id = ownerUserId,
                Name = "Salon Owner",
                Email = "owner.services@email.com",
                PasswordHash = "hash",
                Role = UserRole.SalonOwner,
                SalonId = ownerSalonId,
                IsActive = true
            });

        var token = _factory.CreateToken(ownerUserId, UserRole.SalonOwner.ToString(), salonId: ownerSalonId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/Services", new
        {
            name = " Corte e Escova ",
            description = " Escova modelada ",
            durationInMinutes = 60,
            price = 120m,
            salonId = otherSalonId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<ServiceResponseDto>();
        payload.Should().NotBeNull();
        payload!.SalonId.Should().Be(ownerSalonId);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Services.Should().Contain(service =>
            service.SalonId == ownerSalonId &&
            service.Name == "Corte e Escova");
        dbContext.Services.Should().NotContain(service => service.SalonId == otherSalonId);
    }

    [Fact]
    public async Task Client_Should_SeeActiveService_And_HideInactiveOne()
    {
        await _factory.ResetDatabaseAsync();

        var salonId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var activeServiceId = Guid.NewGuid();
        var inactiveServiceId = Guid.NewGuid();

        await _factory.SeedAsync(
            new Salon { Id = salonId, Name = "Client Salon", Document = "52998224725", Address = "Rua 1" },
            new User
            {
                Id = clientUserId,
                Name = "Client",
                Email = "client.services@email.com",
                PasswordHash = "hash",
                Role = UserRole.Client,
                IsActive = true
            },
            new Service
            {
                Id = activeServiceId,
                SalonId = salonId,
                Name = "Corte",
                Description = "Corte simples",
                DurationInMinutes = 30,
                Price = 50m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Service
            {
                Id = inactiveServiceId,
                SalonId = salonId,
                Name = "Barba",
                Description = "Barba simples",
                DurationInMinutes = 20,
                Price = 30m,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            });

        var token = _factory.CreateToken(clientUserId, UserRole.Client.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var activeResponse = await _client.GetAsync($"/api/Services/{activeServiceId}");
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activePayload = await activeResponse.Content.ReadFromJsonAsync<ServiceResponseDto>();
        activePayload.Should().NotBeNull();
        activePayload!.Id.Should().Be(activeServiceId);

        var inactiveResponse = await _client.GetAsync($"/api/Services/{inactiveServiceId}");
        inactiveResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Client_Should_NotCreateService()
    {
        await _factory.ResetDatabaseAsync();

        var clientUserId = Guid.NewGuid();
        await _factory.SeedAsync(new User
        {
            Id = clientUserId,
            Name = "Client",
            Email = "client.forbidden@email.com",
            PasswordHash = "hash",
            Role = UserRole.Client,
            IsActive = true
        });

        var token = _factory.CreateToken(clientUserId, UserRole.Client.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/Services", new
        {
            name = "Corte",
            description = "Corte simples",
            durationInMinutes = 30,
            price = 50m,
            salonId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SalonOwner_Should_CreateAnd_ListProfessionalAvailability_ForOwnSalon()
    {
        await _factory.ResetDatabaseAsync();

        var salonId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();

        await _factory.SeedAsync(
            new Salon { Id = salonId, Name = "Salon A", Document = "52998224725", Address = "Rua 1" },
            new User
            {
                Id = ownerUserId,
                Name = "Owner",
                Email = "owner.availability@email.com",
                PasswordHash = "hash",
                Role = UserRole.SalonOwner,
                SalonId = salonId,
                IsActive = true
            },
            new User
            {
                Id = professionalId,
                Name = "Professional",
                Email = "pro.availability@email.com",
                PasswordHash = "hash",
                Role = UserRole.Professional,
                SalonId = salonId,
                IsActive = true
            },
            new Professional
            {
                Id = professionalId,
                SalonId = salonId,
                Name = "Professional",
                Email = "pro.availability@email.com",
                IsActive = true
            });

        var token = _factory.CreateToken(ownerUserId, UserRole.SalonOwner.ToString(), salonId: salonId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync($"/api/professionals/{professionalId}/availability", new
        {
            dayOfWeek = DayOfWeek.Monday,
            startTime = new TimeOnly(9, 0),
            endTime = new TimeOnly(12, 0)
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createPayload = await createResponse.Content.ReadFromJsonAsync<ProfessionalAvailabilityResponseDto>();
        createPayload.Should().NotBeNull();
        createPayload!.ProfessionalId.Should().Be(professionalId);
        createPayload.SalonId.Should().Be(salonId);

        var listResponse = await _client.GetAsync($"/api/professionals/{professionalId}/availability");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listPayload = await listResponse.Content.ReadFromJsonAsync<ProfessionalAvailabilityResponseDto[]>();
        listPayload.Should().NotBeNull();
        listPayload!.Should().ContainSingle(item => item.ProfessionalId == professionalId && item.SalonId == salonId);
    }

    [Fact]
    public async Task SalonOwner_Should_NotCreateAvailability_ForProfessional_FromAnotherSalon()
    {
        await _factory.ResetDatabaseAsync();

        var ownerSalonId = Guid.NewGuid();
        var otherSalonId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var otherProfessionalId = Guid.NewGuid();

        await _factory.SeedAsync(
            new Salon { Id = ownerSalonId, Name = "Owner Salon", Document = "52998224725", Address = "Rua 1" },
            new Salon { Id = otherSalonId, Name = "Other Salon", Document = "12345678909", Address = "Rua 2" },
            new User
            {
                Id = ownerUserId,
                Name = "Owner",
                Email = "owner.other@email.com",
                PasswordHash = "hash",
                Role = UserRole.SalonOwner,
                SalonId = ownerSalonId,
                IsActive = true
            },
            new User
            {
                Id = otherProfessionalId,
                Name = "Professional Other",
                Email = "pro.other@email.com",
                PasswordHash = "hash",
                Role = UserRole.Professional,
                SalonId = otherSalonId,
                IsActive = true
            },
            new Professional
            {
                Id = otherProfessionalId,
                SalonId = otherSalonId,
                Name = "Professional Other",
                Email = "pro.other@email.com",
                IsActive = true
            });

        var token = _factory.CreateToken(ownerUserId, UserRole.SalonOwner.ToString(), salonId: ownerSalonId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync($"/api/professionals/{otherProfessionalId}/availability", new
        {
            dayOfWeek = DayOfWeek.Monday,
            startTime = new TimeOnly(9, 0),
            endTime = new TimeOnly(12, 0)
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Professional_Should_NotListAnotherProfessionalAvailability()
    {
        await _factory.ResetDatabaseAsync();

        var salonId = Guid.NewGuid();
        var professionalAId = Guid.NewGuid();
        var professionalBId = Guid.NewGuid();

        await _factory.SeedAsync(
            new Salon { Id = salonId, Name = "Salon A", Document = "52998224725", Address = "Rua 1" },
            new User
            {
                Id = professionalAId,
                Name = "Professional A",
                Email = "pro.a@email.com",
                PasswordHash = "hash",
                Role = UserRole.Professional,
                SalonId = salonId,
                IsActive = true
            },
            new Professional
            {
                Id = professionalAId,
                SalonId = salonId,
                Name = "Professional A",
                Email = "pro.a@email.com",
                IsActive = true
            },
            new User
            {
                Id = professionalBId,
                Name = "Professional B",
                Email = "pro.b@email.com",
                PasswordHash = "hash",
                Role = UserRole.Professional,
                SalonId = salonId,
                IsActive = true
            },
            new Professional
            {
                Id = professionalBId,
                SalonId = salonId,
                Name = "Professional B",
                Email = "pro.b@email.com",
                IsActive = true
            });

        var token = _factory.CreateToken(professionalAId, UserRole.Professional.ToString(), salonId: salonId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/professionals/{professionalBId}/availability");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
