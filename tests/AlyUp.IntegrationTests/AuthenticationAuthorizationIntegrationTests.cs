using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using AlyUp.Infrastructure.Data;
using AlyUp.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AlyUp.IntegrationTests;

public class AuthenticationAuthorizationIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthenticationAuthorizationIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterClient_Should_CreatePublicClientUser()
    {
        await _factory.ResetDatabaseAsync();

        var response = await _client.PostAsJsonAsync("/api/Auth/registerClient", new
        {
            name = "  Cliente Publico  ",
            email = "  Cliente@Email.com  ",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = dbContext.Users.Single();

        user.Role.Should().Be(UserRole.Client);
        user.SalonId.Should().BeNull();
        user.Email.Should().Be("cliente@email.com");
        user.PasswordHash.Should().NotBe("Password123!");
    }

    [Fact]
    public async Task RegisterClient_Should_ValidateInvalidPayload()
    {
        await _factory.ResetDatabaseAsync();

        var response = await _client.PostAsJsonAsync("/api/Auth/registerClient", new
        {
            name = "",
            email = "invalid-email",
            password = "123"
        });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("errors");
    }

    [Fact]
    public async Task Login_Should_ReturnTokenWithoutSensitiveClaims()
    {
        await _factory.ResetDatabaseAsync();

        var passwordHasher = new BCryptPasswordHasher();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Client User",
            Email = "client@email.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Client,
            IsActive = true
        };

        await _factory.SeedAsync(user);

        var response = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "  Client@Email.com  ",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        payload.Should().NotBeNull();
        payload!.UserId.Should().Be(user.Id);
        payload.Role.Should().Be(UserRole.Client);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.Token);
        jwt.Claims.Should().Contain(claim => claim.Type == "UserId" && claim.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(claim => claim.Type == "Role" && claim.Value == UserRole.Client.ToString());
        jwt.Claims.Should().NotContain(claim => claim.Type == "Email");
        jwt.Claims.Should().NotContain(claim => claim.Type == "Name");
        jwt.Claims.Should().NotContain(claim => claim.Type.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_Should_ReturnUnauthorized_When_UserIsInactive()
    {
        await _factory.ResetDatabaseAsync();

        var passwordHasher = new BCryptPasswordHasher();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Inactive User",
            Email = "inactive@email.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Client,
            IsActive = false
        };

        await _factory.SeedAsync(user);

        var response = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "inactive@email.com",
            password = "Password123!"
        });

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.Should().Contain("Email ou senha invalidos.");
    }

    [Fact]
    public async Task AdminEndpoint_Should_RequireMaster()
    {
        await _factory.ResetDatabaseAsync();

        var adminToken = _factory.CreateToken(Guid.NewGuid(), UserRole.Admin.ToString(), isMaster: false);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.PostAsJsonAsync("/api/Admin/registerSalonOwner", new
        {
            name = "Owner",
            email = "owner@email.com",
            password = "Password123!",
            salonName = "Salao A",
            salonDocument = "123456789",
            salonAddress = "Rua 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_Should_AllowMaster_ToCreateSalonOwner()
    {
        await _factory.ResetDatabaseAsync();

        var masterToken = _factory.CreateToken(Guid.NewGuid(), UserRole.Admin.ToString(), isMaster: true);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);

        var response = await _client.PostAsJsonAsync("/api/Admin/registerSalonOwner", new
        {
            name = "Owner",
            email = "owner@email.com",
            password = "Password123!",
            salonName = "Salao A",
            salonDocument = "123456789",
            salonAddress = "Rua 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Users.Should().Contain(user => user.Email == "owner@email.com" && user.Role == UserRole.SalonOwner);
        dbContext.Salons.Should().Contain(salon => salon.Document == "123456789");
    }

    [Fact]
    public async Task SalonOwner_Should_CreateProfessional_OnlyForOwnSalon()
    {
        await _factory.ResetDatabaseAsync();

        var ownerSalonId = Guid.NewGuid();
        var otherSalonId = Guid.NewGuid();

        await _factory.SeedAsync(
            new Salon { Id = ownerSalonId, Name = "Salao Dono", Document = "111", Address = "Rua 1" },
            new Salon { Id = otherSalonId, Name = "Salao Outro", Document = "222", Address = "Rua 2" });

        var ownerToken = _factory.CreateToken(Guid.NewGuid(), UserRole.SalonOwner.ToString(), salonId: ownerSalonId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await _client.PostAsJsonAsync("/api/Salon/createProfessionals", new
        {
            name = "Professional One",
            email = "professional@email.com",
            password = "Password123!",
            salonId = otherSalonId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Users.Should().Contain(user =>
            user.Email == "professional@email.com" &&
            user.Role == UserRole.Professional &&
            user.SalonId == ownerSalonId);
    }

    [Fact]
    public async Task Master_Should_CreateProfessional_WhenSalonIdIsProvided()
    {
        await _factory.ResetDatabaseAsync();

        var salonId = Guid.NewGuid();
        await _factory.SeedAsync(new Salon { Id = salonId, Name = "Salao Master", Document = "333", Address = "Rua 3" });

        var masterToken = _factory.CreateToken(Guid.NewGuid(), UserRole.Admin.ToString(), isMaster: true);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);

        var response = await _client.PostAsJsonAsync("/api/Salon/createProfessionals", new
        {
            name = "Professional Master",
            email = "professional.master@email.com",
            password = "Password123!",
            salonId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Master_Should_NotCreateProfessional_WithoutSalonId()
    {
        await _factory.ResetDatabaseAsync();

        var masterToken = _factory.CreateToken(Guid.NewGuid(), UserRole.Admin.ToString(), isMaster: true);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);

        var response = await _client.PostAsJsonAsync("/api/Salon/createProfessionals", new
        {
            name = "Professional Master",
            email = "professional.master@email.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/api/Admin/registerSalonOwner")]
    [InlineData("/api/Salon/createProfessionals")]
    public async Task Client_Should_NotAccess_PrivilegedEndpoints(string url)
    {
        await _factory.ResetDatabaseAsync();

        var clientToken = _factory.CreateToken(Guid.NewGuid(), UserRole.Client.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

        var response = await _client.PostAsJsonAsync(url, new
        {
            name = "Any",
            email = "any@email.com",
            password = "Password123!",
            salonName = "Salao A",
            salonDocument = "123456789",
            salonAddress = "Rua 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("WrongAudience", "AlyUp.IntegrationTests", "wrong-audience", null)]
    [InlineData("WrongIssuer", "wrong-issuer", "AlyUp.Clients", null)]
    [InlineData("WrongSignature", "AlyUp.IntegrationTests", "AlyUp.Clients", "another-signing-key-with-32-chars!!!")]
    public async Task PrivilegedEndpoints_Should_RejectInvalidTokens(
        string _,
        string issuer,
        string audience,
        string? signingKey)
    {
        await _factory.ResetDatabaseAsync();

        var token = _factory.CreateToken(
            Guid.NewGuid(),
            UserRole.Admin.ToString(),
            isMaster: true,
            issuer: issuer,
            audience: audience,
            signingKey: signingKey);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/Admin/registerSalonOwner", new
        {
            name = "Owner",
            email = "owner@email.com",
            password = "Password123!",
            salonName = "Salao A",
            salonDocument = "123456789",
            salonAddress = "Rua 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PrivilegedEndpoints_Should_RejectExpiredToken()
    {
        await _factory.ResetDatabaseAsync();

        var token = _factory.CreateToken(
            Guid.NewGuid(),
            UserRole.Admin.ToString(),
            isMaster: true,
            expires: DateTime.UtcNow.AddMinutes(-5));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/Admin/registerSalonOwner", new
        {
            name = "Owner",
            email = "owner@email.com",
            password = "Password123!",
            salonName = "Salao A",
            salonDocument = "123456789",
            salonAddress = "Rua 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
