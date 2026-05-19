using AlyUp.Domain.Entities;
using AlyUp.Application.Interfaces;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AlyUp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private static readonly TimeSpan AppUtcOffset = TimeSpan.FromHours(-3);
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : this(options, new NoTenantContext())
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Salon> Salons => Set<Salon>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureClient(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureSalon(modelBuilder);
        ConfigureService(modelBuilder);
        ConfigureAppointment(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
        ConfigureAuditDateColumns(modelBuilder);
        ApplyTenantQueryFilters(modelBuilder);
        ApplySnakeCaseNamingConvention(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyAuditDateTimeRules();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditDateTimeRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditDateTimeRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditDateTimeRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private bool ShouldApplyTenantFilter => _tenantContext.ShouldApplyTenantFilter;
    private Guid TenantSalonId => _tenantContext.SalonId ?? Guid.Empty;

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>()
            .HasQueryFilter(entity => !ShouldApplyTenantFilter || entity.SalonId == TenantSalonId);

        modelBuilder.Entity<Service>()
            .HasQueryFilter(entity => !ShouldApplyTenantFilter || entity.SalonId == TenantSalonId);

        modelBuilder.Entity<Appointment>()
            .HasQueryFilter(entity => !ShouldApplyTenantFilter || entity.SalonId == TenantSalonId);
    }

    private static void ConfigureClient(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Client>()
            .ToTable("clients")
            .HasKey(c => c.Id);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.Phone)
            .HasMaxLength(20);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.Email)
            .HasMaxLength(150);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.Notes)
            .HasMaxLength(500);

        modelBuilder
            .Entity<Client>()
            .Property(c => c.CreatedAt)
            .IsRequired();

        modelBuilder
            .Entity<Client>()
            .HasOne(c => c.Salon)
            .WithMany(s => s.Clients)
            .HasForeignKey(c => c.SalonId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<User>();
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Name).IsRequired().HasMaxLength(150);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.IsActive).IsRequired().ValueGeneratedNever();

        builder.HasOne(u => u.Salon)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SalonId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureSalon(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Salon>();
        builder.ToTable("salons");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.Property(s => s.Document).IsRequired().HasMaxLength(20);
        builder.HasIndex(s => s.Document).IsUnique();
        builder.Property(s => s.Address).IsRequired().HasMaxLength(250);
    }

    private static void ConfigureService(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Service>();
        builder.ToTable("services");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.Property(s => s.Price).IsRequired();
        builder.Property(s => s.DurationInMinutes).IsRequired();

        builder.HasOne(s => s.Salon)
            .WithMany(salon => salon.Services)
            .HasForeignKey(s => s.SalonId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAppointment(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<Appointment>();
        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.StartDateTime).IsRequired();
        builder.Property(a => a.EndDateTime).IsRequired();
        builder.Property(a => a.Price).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasOne(a => a.Salon)
            .WithMany(salon => salon.Appointments)
            .HasForeignKey(a => a.SalonId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<RefreshToken>();
        builder.ToTable("refresh_tokens");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.SessionId).IsRequired();
        builder.Property(rt => rt.FamilyId).IsRequired();
        builder.Property(rt => rt.TokenHash).IsRequired();
        builder.HasIndex(rt => rt.TokenHash).IsUnique();
        builder.HasIndex(rt => rt.FamilyId);
        builder.HasIndex(rt => rt.SessionId);
        builder.Property(rt => rt.Created).IsRequired();
        builder.Property(rt => rt.Expires).IsRequired();
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);
    }

    private static void ConfigureAuditDateColumns(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                var isAuditDateProperty =
                    property.Name is "CreatedAt" or "UpdatedAt" &&
                    (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?));

                if (isAuditDateProperty)
                {
                    property.SetColumnType("timestamp without time zone");
                }
            }
        }
    }

    private void ApplyAuditDateTimeRules()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            ApplyCreatedAtRule(entry);
            ApplyUpdatedAtRule(entry);
        }
    }

    private void ApplyCreatedAtRule(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var createdAtProperty = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "CreatedAt");
        if (createdAtProperty is null)
        {
            return;
        }

        if (entry.State == EntityState.Added)
        {
            var currentValue = createdAtProperty.CurrentValue as DateTime?;
            createdAtProperty.CurrentValue = ConvertToAppLocalDateTime(currentValue ?? DateTime.UtcNow);
            return;
        }

        createdAtProperty.IsModified = false;
    }

    private void ApplyUpdatedAtRule(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var updatedAtProperty = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "UpdatedAt");
        if (updatedAtProperty is null)
        {
            return;
        }

        if (entry.State == EntityState.Modified)
        {
            updatedAtProperty.CurrentValue = ConvertToAppLocalDateTime(DateTime.UtcNow);
            return;
        }

        var currentValue = updatedAtProperty.CurrentValue as DateTime?;
        if (currentValue.HasValue)
        {
            updatedAtProperty.CurrentValue = ConvertToAppLocalDateTime(currentValue.Value);
        }
    }

    private static DateTime ConvertToAppLocalDateTime(DateTime value)
    {
        var normalizedUtc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        var localValue = normalizedUtc + AppUtcOffset;
        return DateTime.SpecifyKind(localValue, DateTimeKind.Unspecified);
    }

    private static void ApplySnakeCaseNamingConvention(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entity.GetProperties())
            {
                var storeObjectIdentifier = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
                var columnName = property.GetColumnName(storeObjectIdentifier);

                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    property.SetColumnName(ToSnakeCase(columnName));
                }
            }

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrWhiteSpace(keyName))
                {
                    key.SetName(ToSnakeCase(keyName));
                }
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var constraintName = foreignKey.GetConstraintName();
                if (!string.IsNullOrWhiteSpace(constraintName))
                {
                    foreignKey.SetConstraintName(ToSnakeCase(constraintName));
                }
            }

            foreach (var index in entity.GetIndexes())
            {
                var databaseName = index.GetDatabaseName();
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    index.SetDatabaseName(ToSnakeCase(databaseName));
                }
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var result = new System.Text.StringBuilder(value.Length + Math.Min(2, value.Length / 5));
        var previousCategory = default(UnicodeCategory?);

        for (var i = 0; i < value.Length; i++)
        {
            var currentChar = value[i];
            if (currentChar == '_')
            {
                result.Append(currentChar);
                previousCategory = null;
                continue;
            }

            var currentCategory = char.GetUnicodeCategory(currentChar);
            if (currentCategory == UnicodeCategory.UppercaseLetter)
            {
                var hasPrevious = i > 0;
                var hasNext = i + 1 < value.Length;
                var nextIsLower = hasNext && char.IsLower(value[i + 1]);

                if (hasPrevious &&
                    previousCategory != UnicodeCategory.SpaceSeparator &&
                    previousCategory != null &&
                    previousCategory != UnicodeCategory.ConnectorPunctuation &&
                    (previousCategory != UnicodeCategory.UppercaseLetter || nextIsLower))
                {
                    result.Append('_');
                }

                result.Append(char.ToLowerInvariant(currentChar));
            }
            else
            {
                result.Append(char.ToLowerInvariant(currentChar));
            }

            previousCategory = currentCategory;
        }

        return result.ToString();
    }

    private sealed class NoTenantContext : ITenantContext
    {
        public bool ShouldApplyTenantFilter => false;
        public Guid? SalonId => null;
    }
}
