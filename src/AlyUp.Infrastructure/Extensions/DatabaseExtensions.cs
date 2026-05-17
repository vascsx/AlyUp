using AlyUp.Application.Interfaces;
using AlyUp.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlyUp.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("Database:SkipMigrationsAndSeed"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();

        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

        var db = services.GetRequiredService<AppDbContext>();

        await FixEfMigrationsHistoryNamingAsync(db, logger);

        var migrations = db.Database.GetMigrations();
        if (migrations.Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            logger.LogWarning("No EF Core migrations found. Using Database.EnsureCreated().");
            await db.Database.EnsureCreatedAsync();
        }

        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var inputNormalizer = services.GetRequiredService<IInputNormalizer>();
        await MasterUserSeeder.SeedAsync(db, passwordHasher, inputNormalizer, app.Configuration, logger);
    }

    private static async Task FixEfMigrationsHistoryNamingAsync(AppDbContext db, ILogger logger)
    {
        try
        {
            var historyTableExists = await db.Database
                .SqlQueryRaw<bool>("""
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = '__EFMigrationsHistory'
                    );
                    """)
                .SingleAsync();

            if (!historyTableExists)
            {
                return;
            }

            var hasPascalMigrationId = await db.Database
                .SqlQueryRaw<bool>("""
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '__EFMigrationsHistory'
                          AND column_name = 'MigrationId'
                    );
                    """)
                .SingleAsync();

            var hasPascalProductVersion = await db.Database
                .SqlQueryRaw<bool>("""
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '__EFMigrationsHistory'
                          AND column_name = 'ProductVersion'
                    );
                    """)
                .SingleAsync();

            if (hasPascalMigrationId)
            {
                logger.LogInformation("Renaming __EFMigrationsHistory.MigrationId -> migration_id");
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"__EFMigrationsHistory\" RENAME COLUMN \"MigrationId\" TO migration_id;");
            }

            if (hasPascalProductVersion)
            {
                logger.LogInformation("Renaming __EFMigrationsHistory.ProductVersion -> product_version");
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"__EFMigrationsHistory\" RENAME COLUMN \"ProductVersion\" TO product_version;");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fix __EFMigrationsHistory naming. Migration may fail until it's corrected.");
        }
    }
}
