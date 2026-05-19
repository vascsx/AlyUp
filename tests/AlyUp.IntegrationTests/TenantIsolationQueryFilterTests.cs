using AlyUp.Application.Interfaces;
using AlyUp.Domain.Entities;
using AlyUp.Domain.Enums;
using AlyUp.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AlyUp.IntegrationTests;

public class TenantIsolationQueryFilterTests
{
    [Fact]
    public async Task Should_FilterSalonScopedAggregates_WhenTenantFilterIsEnabled()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var salonA = Guid.NewGuid();
        var salonB = Guid.NewGuid();

        await using var context = CreateContext(connection, new FakeTenantContext(true, salonA));
        await context.Database.EnsureCreatedAsync();

        await SeedSalonScopedDataAsync(context, salonA, salonB);

        var clients = await context.Clients.ToListAsync();
        var services = await context.Services.ToListAsync();
        var appointments = await context.Appointments.ToListAsync();

        clients.Should().OnlyContain(entity => entity.SalonId == salonA);
        services.Should().OnlyContain(entity => entity.SalonId == salonA);
        appointments.Should().OnlyContain(entity => entity.SalonId == salonA);
    }

    [Fact]
    public async Task Should_NotFilterSalonScopedAggregates_WhenTenantFilterIsDisabled()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var salonA = Guid.NewGuid();
        var salonB = Guid.NewGuid();

        await using var context = CreateContext(connection, new FakeTenantContext(false, null));
        await context.Database.EnsureCreatedAsync();

        await SeedSalonScopedDataAsync(context, salonA, salonB);

        var clients = await context.Clients.ToListAsync();
        var services = await context.Services.ToListAsync();
        var appointments = await context.Appointments.ToListAsync();

        clients.Should().HaveCount(2);
        services.Should().HaveCount(2);
        appointments.Should().HaveCount(2);
    }

    private static AppDbContext CreateContext(SqliteConnection connection, ITenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    private static async Task SeedSalonScopedDataAsync(AppDbContext context, Guid salonA, Guid salonB)
    {
        var clientAId = Guid.NewGuid();
        var clientBId = Guid.NewGuid();
        var serviceAId = Guid.NewGuid();
        var serviceBId = Guid.NewGuid();

        await context.Salons.AddRangeAsync(
            new Salon { Id = salonA, Name = "Salon A", Document = "111", Address = "Rua A" },
            new Salon { Id = salonB, Name = "Salon B", Document = "222", Address = "Rua B" });

        await context.Clients.AddRangeAsync(
            new Client { Id = clientAId, SalonId = salonA, Name = "Client A", CreatedAt = DateTime.UtcNow },
            new Client { Id = clientBId, SalonId = salonB, Name = "Client B", CreatedAt = DateTime.UtcNow });

        await context.Services.AddRangeAsync(
            new Service { Id = serviceAId, SalonId = salonA, Name = "Service A", Price = 10m, DurationInMinutes = 30, CreatedAt = DateTime.UtcNow },
            new Service { Id = serviceBId, SalonId = salonB, Name = "Service B", Price = 12m, DurationInMinutes = 40, CreatedAt = DateTime.UtcNow });

        await context.Appointments.AddRangeAsync(
            new Appointment
            {
                Id = Guid.NewGuid(),
                SalonId = salonA,
                ClientId = clientAId,
                ServiceId = serviceAId,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddMinutes(30),
                Price = 10m,
                Status = AppointmentStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                SalonId = salonB,
                ClientId = clientBId,
                ServiceId = serviceBId,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddMinutes(30),
                Price = 12m,
                Status = AppointmentStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(bool shouldApplyTenantFilter, Guid? salonId)
        {
            ShouldApplyTenantFilter = shouldApplyTenantFilter;
            SalonId = salonId;
        }

        public bool ShouldApplyTenantFilter { get; }
        public Guid? SalonId { get; }
    }
}
