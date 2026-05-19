using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlyUp.Application.DTOs.Auth;
using AlyUp.Application.Interfaces;
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

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterClient_Should_ReturnConflict_When_EmailAlreadyExists()
    {
        await _factory.ResetDatabaseAsync();

        var passwordHasher = new BCryptPasswordHasher();
        await _factory.SeedAsync(new User
        {
            Id = Guid.NewGuid(),
            Name = "Existing Client",
            Email = "existing.client@email.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Client,
            IsActive = true
        });

        var response = await _client.PostAsJsonAsync("/api/Auth/registerClient", new
        {
            name = "New Client",
            email = " existing.client@email.com ",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_Should_ReturnAccessAndRefreshTokens_WithoutSensitiveClaims()
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
        payload.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.Token);
        jwt.Claims.Should().Contain(claim => claim.Type == "UserId" && claim.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(claim => claim.Type == "Role" && claim.Value == UserRole.Client.ToString());
        jwt.Claims.Should().NotContain(claim => claim.Type == "Email");
        jwt.Claims.Should().NotContain(claim => claim.Type == "Name");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var refreshTokenHasher = scope.ServiceProvider.GetRequiredService<IRefreshTokenHasher>();
        dbContext.RefreshTokens.Should().Contain(token => token.UserId == user.Id && token.TokenHash == refreshTokenHasher.Hash(payload.RefreshToken) && token.Revoked == null);
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

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Should_ReturnTooManyRequests_WhenRateLimitIsExceeded()
    {
        await using var localFactory = new TestWebApplicationFactory();
        await localFactory.ResetDatabaseAsync();

        using var localClient = localFactory.CreateClient();
        var passwordHasher = new BCryptPasswordHasher();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Rate Limited User",
            Email = "rate.limit@email.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Client,
            IsActive = true
        };

        await localFactory.SeedAsync(user);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await localClient.PostAsJsonAsync("/api/Auth/login", new
            {
                email = "rate.limit@email.com",
                password = "Password123!"
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var rateLimitedResponse = await localClient.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "rate.limit@email.com",
            password = "Password123!"
        });

        rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_Should_NotShareRateLimitBetweenDifferentEmails_OnSameIp()
    {
        await _factory.ResetDatabaseAsync();

        var passwordHasher = new BCryptPasswordHasher();
        var firstUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "First User",
            Email = "first@email.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Client,
            IsActive = true
        };

        var secondUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Second User",
            Email = "second@email.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Client,
            IsActive = true
        };

        await _factory.SeedAsync(firstUser, secondUser);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await _client.PostAsJsonAsync("/api/Auth/login", new
            {
                email = "first@email.com",
                password = "Password123!"
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var secondUserResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = " second@email.com ",
            password = "Password123!"
        });

        secondUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_Should_RotateRefreshToken_And_RevokePreviousOne()
    {
        await _factory.ResetDatabaseAsync();

        var passwordHasher = new BCryptPasswordHasher();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Refresh User",
            Email = "refresh@email.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Client,
            IsActive = true
        };

        await _factory.SeedAsync(user);

        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "refresh@email.com",
            password = "Password123!"
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginPayload.Should().NotBeNull();

        var refreshResponse = await _client.PostAsJsonAsync("/api/Auth/refresh", new
        {
            refreshToken = loginPayload!.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponseDto>();
        refreshPayload.Should().NotBeNull();
        refreshPayload!.RefreshToken.Should().NotBe(loginPayload.RefreshToken);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var refreshTokenHasher = scope.ServiceProvider.GetRequiredService<IRefreshTokenHasher>();
        dbContext.RefreshTokens.Should().Contain(token => token.TokenHash == refreshTokenHasher.Hash(loginPayload.RefreshToken) && token.Revoked != null);
        dbContext.RefreshTokens.Should().Contain(token => token.TokenHash == refreshTokenHasher.Hash(refreshPayload.RefreshToken) && token.Revoked == null);
    }

    [Fact]
    public async Task Logout_Should_RevokeRefreshToken_And_BlockFurtherRefresh()
    {
        await _factory.ResetDatabaseAsync();

        var passwordHasher = new BCryptPasswordHasher();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Logout User",
            Email = "logout@email.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Client,
            IsActive = true
        };

        await _factory.SeedAsync(user);

        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "logout@email.com",
            password = "Password123!"
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginPayload.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Token);

        var logoutResponse = await _client.PostAsJsonAsync("/api/Auth/logout", new
        {
            refreshToken = loginPayload.RefreshToken
        });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshResponse = await _client.PostAsJsonAsync("/api/Auth/refresh", new
        {
            refreshToken = loginPayload.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Should_KeepPreviouslyIssuedAccessToken_Valid_Until_Expiry()
    {
        await _factory.ResetDatabaseAsync();

        var passwordHasher = new BCryptPasswordHasher();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Logout Access User",
            Email = "logout.access@email.com",
            PasswordHash = passwordHasher.Hash("Password123!"),
            Role = UserRole.Client,
            IsActive = true
        };

        await _factory.SeedAsync(user);

        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "logout.access@email.com",
            password = "Password123!"
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginPayload.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Token);

        var logoutResponse = await _client.PostAsJsonAsync("/api/Auth/logout", new
        {
            refreshToken = loginPayload.RefreshToken
        });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var profileResponse = await _client.GetAsync("/api/Client/me");

        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_Should_RequireAuthentication()
    {
        await _factory.ResetDatabaseAsync();

        var response = await _client.PostAsJsonAsync("/api/Auth/logout", new
        {
            refreshToken = "any-refresh-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_Should_RequireMaster()
    {
        await _factory.ResetDatabaseAsync();

        var adminUserId = Guid.NewGuid();
        await _factory.SeedAsync(new User
        {
            Id = adminUserId,
            Name = "Admin User",
            Email = "admin@email.com",
            PasswordHash = "hash",
            Role = UserRole.Admin,
            IsActive = true
        });

        var adminToken = _factory.CreateToken(adminUserId, UserRole.Admin.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.PostAsJsonAsync("/api/Admin/registerSalonOwner", new
        {
            name = "Owner",
            email = "owner@email.com",
            password = "Password123!",
            salonName = "Salao A",
            salonDocument = "52998224725",
            salonAddress = "Rua 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_Should_AllowMaster_ToCreateSalonOwner()
    {
        await _factory.ResetDatabaseAsync();

        var masterUserId = Guid.NewGuid();
        await _factory.SeedAsync(new User
        {
            Id = masterUserId,
            Name = "Master User",
            Email = "master@email.com",
            PasswordHash = "hash",
            Role = UserRole.Master,
            IsActive = true
        });

        var masterToken = _factory.CreateToken(masterUserId, UserRole.Master.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);

        var response = await _client.PostAsJsonAsync("/api/Admin/registerSalonOwner", new
        {
            name = "  Owner   Name  ",
            email = "owner@email.com",
            password = "Password123!",
            salonName = "  Salao   A  ",
            salonDocument = "529.982.247-25",
            salonAddress = "  Rua   1  "
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Salons.Should().Contain(salon =>
            salon.Document == "52998224725" &&
            salon.Name == "Salao A" &&
            salon.Address == "Rua 1");

        dbContext.Users.Should().Contain(user =>
            user.Email == "owner@email.com" &&
            user.Name == "Owner Name");
    }

    [Fact]
    public async Task AdminEndpoint_Should_ReturnConflict_WhenSalonOwnerOrSalonAlreadyExists()
    {
        await _factory.ResetDatabaseAsync();

        var masterUserId = Guid.NewGuid();
        await _factory.SeedAsync(
            new User
            {
                Id = masterUserId,
                Name = "Master User",
                Email = "master.conflict@email.com",
                PasswordHash = "hash",
                Role = UserRole.Master,
                IsActive = true
            },
            new Salon
            {
                Id = Guid.NewGuid(),
                Name = "Existing Salon",
                Document = "52998224725",
                Address = "Rua 1"
            });

        var masterToken = _factory.CreateToken(masterUserId, UserRole.Master.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);

        var response = await _client.PostAsJsonAsync("/api/Admin/registerSalonOwner", new
        {
            name = "Owner",
            email = "owner@email.com",
            password = "Password123!",
            salonName = "Salao A",
            salonDocument = "529.982.247-25",
            salonAddress = "Rua 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SalonOwner_Should_CreateProfessional_OnlyForOwnSalon()
    {
        await _factory.ResetDatabaseAsync();

        var ownerSalonId = Guid.NewGuid();
        var otherSalonId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        await _factory.SeedAsync(
            new Salon { Id = ownerSalonId, Name = "Salao Dono", Document = "111", Address = "Rua 1" },
            new Salon { Id = otherSalonId, Name = "Salao Outro", Document = "222", Address = "Rua 2" },
            new User
            {
                Id = ownerUserId,
                Name = "Owner User",
                Email = "owner@email.com",
                PasswordHash = "hash",
                Role = UserRole.SalonOwner,
                SalonId = ownerSalonId,
                IsActive = true
            });

        var ownerToken = _factory.CreateToken(ownerUserId, UserRole.SalonOwner.ToString(), salonId: ownerSalonId);
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
        var masterUserId = Guid.NewGuid();
        await _factory.SeedAsync(
            new Salon { Id = salonId, Name = "Salao Master", Document = "333", Address = "Rua 3" },
            new User
            {
                Id = masterUserId,
                Name = "Master User",
                Email = "master.professional@email.com",
                PasswordHash = "hash",
                Role = UserRole.Master,
                IsActive = true
            });

        var masterToken = _factory.CreateToken(masterUserId, UserRole.Master.ToString());
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

        var masterUserId = Guid.NewGuid();
        await _factory.SeedAsync(new User
        {
            Id = masterUserId,
            Name = "Master User",
            Email = "master.no.salon@email.com",
            PasswordHash = "hash",
            Role = UserRole.Master,
            IsActive = true
        });

        var masterToken = _factory.CreateToken(masterUserId, UserRole.Master.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);

        var response = await _client.PostAsJsonAsync("/api/Salon/createProfessionals", new
        {
            name = "Professional Master",
            email = "professional.master@email.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProfessionalEndpoint_Should_ReturnOnlyCurrentProfessional()
    {
        await _factory.ResetDatabaseAsync();

        var salonId = Guid.NewGuid();
        var professionalUserId = Guid.NewGuid();
        await _factory.SeedAsync(
            new Salon { Id = salonId, Name = "Salao Professional", Document = "444", Address = "Rua 4" },
            new User
            {
                Id = professionalUserId,
                Name = "Professional User",
                Email = "professional.user@email.com",
                PasswordHash = "hash",
                Role = UserRole.Professional,
                SalonId = salonId,
                IsActive = true
            });

        var token = _factory.CreateToken(professionalUserId, UserRole.Professional.ToString(), salonId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Professional/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(professionalUserId);
        payload.Role.Should().Be(UserRole.Professional);
        payload.SalonId.Should().Be(salonId);
    }

    [Fact]
    public async Task ClientEndpoint_Should_ReturnOnlyCurrentClientUser()
    {
        await _factory.ResetDatabaseAsync();

        var clientUserId = Guid.NewGuid();
        await _factory.SeedAsync(new User
        {
            Id = clientUserId,
            Name = "Client User",
            Email = "client.user@email.com",
            PasswordHash = "hash",
            Role = UserRole.Client,
            IsActive = true
        });

        var token = _factory.CreateToken(clientUserId, UserRole.Client.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Client/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(clientUserId);
        payload.Role.Should().Be(UserRole.Client);
    }

    [Theory]
    [InlineData("/api/Admin/registerSalonOwner")]
    [InlineData("/api/Salon/createProfessionals")]
    [InlineData("/api/Professional/me")]
    public async Task Client_Should_NotAccess_PrivilegedEndpoints(string url)
    {
        await _factory.ResetDatabaseAsync();

        var clientUserId = Guid.NewGuid();
        await _factory.SeedAsync(new User
        {
            Id = clientUserId,
            Name = "Client User",
            Email = $"client.{Guid.NewGuid():N}@email.com",
            PasswordHash = "hash",
            Role = UserRole.Client,
            IsActive = true
        });

        var clientToken = _factory.CreateToken(clientUserId, UserRole.Client.ToString());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

        var response = url.Contains("/me", StringComparison.Ordinal)
            ? await _client.GetAsync(url)
            : await _client.PostAsJsonAsync(url, new
            {
                name = "Any",
                email = "any@email.com",
                password = "Password123!",
                salonName = "Salao A",
                salonDocument = "52998224725",
                salonAddress = "Rua 1"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/Client/me")]
    [InlineData("/api/Auth/refresh")]
    public async Task Professional_Should_NotAccess_ClientOnlyEndpoints(string url)
    {
        await _factory.ResetDatabaseAsync();

        var salonId = Guid.NewGuid();
        var professionalUserId = Guid.NewGuid();
        await _factory.SeedAsync(
            new Salon { Id = salonId, Name = "Salao Professional Forbidden", Document = Guid.NewGuid().ToString("N")[..10], Address = "Rua 5" },
            new User
            {
                Id = professionalUserId,
                Name = "Professional User",
                Email = $"professional.{Guid.NewGuid():N}@email.com",
                PasswordHash = "hash",
                Role = UserRole.Professional,
                SalonId = salonId,
                IsActive = true
            });

        var professionalToken = _factory.CreateToken(professionalUserId, UserRole.Professional.ToString(), salonId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", professionalToken);

        var response = url.EndsWith("/me", StringComparison.Ordinal)
            ? await _client.GetAsync(url)
            : await _client.PostAsJsonAsync(url, new { refreshToken = "invalid" });

        if (url.EndsWith("/me", StringComparison.Ordinal))
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        else
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("wrong-audience", "AlyUp.IntegrationTests", null)]
    [InlineData("AlyUp.Clients", "wrong-issuer", null)]
    [InlineData("AlyUp.Clients", "AlyUp.IntegrationTests", "another-signing-key-with-32-chars!!!")]
    public async Task PrivilegedEndpoints_Should_RejectInvalidTokens(
        string audience,
        string issuer,
        string? signingKey)
    {
        await _factory.ResetDatabaseAsync();

        var token = _factory.CreateToken(
            Guid.NewGuid(),
            UserRole.Master.ToString(),
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
            salonDocument = "52998224725",
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
            UserRole.Master.ToString(),
            expires: DateTime.UtcNow.AddMinutes(-5));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/Admin/registerSalonOwner", new
        {
            name = "Owner",
            email = "owner@email.com",
            password = "Password123!",
            salonName = "Salao A",
            salonDocument = "52998224725",
            salonAddress = "Rua 1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
