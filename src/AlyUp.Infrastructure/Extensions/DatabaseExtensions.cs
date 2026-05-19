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
        using var scope = app.Services.CreateScope();

        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

        var db = services.GetRequiredService<AppDbContext>();

        await FixEfMigrationsHistoryNamingAsync(db, logger);

        var migrations = db.Database.GetMigrations();
        if (!migrations.Any())
        {
            throw new InvalidOperationException(
                "No EF Core migrations were found for this application. Create migrations manually before running database update.");
        }

        await db.Database.MigrateAsync();

        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var inputNormalizer = services.GetRequiredService<IInputNormalizer>();
        await MasterUserSeeder.SeedAsync(db, passwordHasher, inputNormalizer, app.Configuration, logger);
    }

    private static async Task FixEfMigrationsHistoryNamingAsync(AppDbContext db, ILogger logger)
    {
        try
        {
            var legacyHistoryTableExists = await db.Database
                .SqlQueryRaw<bool>("""
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = '__EFMigrationsHistory'
                    )
                    """)
                .SingleAsync();

            if (legacyHistoryTableExists)
            {
                logger.LogInformation("Renaming __EFMigrationsHistory -> __ef_migrations_history");
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"__EFMigrationsHistory\" RENAME TO __ef_migrations_history;");
            }

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS __ef_migrations_history (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId")
                );
                """);

            var historyTableExists = await db.Database
                .SqlQueryRaw<bool>("""
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = '__ef_migrations_history'
                    )
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
                          AND table_name = '__ef_migrations_history'
                          AND column_name = 'MigrationId'
                    )
                    """)
                .SingleAsync();

            var hasPascalProductVersion = await db.Database
                .SqlQueryRaw<bool>("""
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '__ef_migrations_history'
                          AND column_name = 'ProductVersion'
                    )
                    """)
                .SingleAsync();

            if (hasPascalMigrationId)
            {
                logger.LogInformation("EF migrations history already uses MigrationId.");
            }

            var hasSnakeMigrationId = await db.Database
                .SqlQueryRaw<bool>("""
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '__ef_migrations_history'
                          AND column_name = 'migration_id'
                    )
                    """)
                .SingleAsync();

            var hasSnakeProductVersion = await db.Database
                .SqlQueryRaw<bool>("""
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '__ef_migrations_history'
                          AND column_name = 'product_version'
                    )
                    """)
                .SingleAsync();

            if (hasSnakeMigrationId)
            {
                logger.LogInformation("Renaming __ef_migrations_history.migration_id -> MigrationId");
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE __ef_migrations_history RENAME COLUMN migration_id TO \"MigrationId\";");
            }

            if (hasPascalProductVersion)
            {
                logger.LogInformation("EF migrations history already uses ProductVersion.");
            }

            if (hasSnakeProductVersion)
            {
                logger.LogInformation("Renaming __ef_migrations_history.product_version -> ProductVersion");
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE __ef_migrations_history RENAME COLUMN product_version TO \"ProductVersion\";");
            }

            await BaselineExistingSchemaAsync(db, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fix EF migrations history naming. Migration may fail until it's corrected.");
        }
    }

    private static async Task BaselineExistingSchemaAsync(AppDbContext db, ILogger logger)
    {
        var historyCount = await db.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int
                FROM __ef_migrations_history
                """)
            .SingleAsync();

        if (historyCount > 0)
        {
            return;
        }

        var hasCoreSchema = await db.Database
            .SqlQueryRaw<bool>(
                """
                SELECT COUNT(*) = 3
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name IN ('salons', 'users', 'refresh_tokens')
                """)
            .SingleAsync();

        if (!hasCoreSchema)
        {
            return;
        }

        logger.LogInformation("Existing schema detected without EF migrations history. Baselineing InitialCreate migration.");

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO __ef_migrations_history ("MigrationId", "ProductVersion")
            SELECT '20260518001039_InitialCreate', '10.0.8'
            WHERE NOT EXISTS (
                SELECT 1
                FROM __ef_migrations_history
                WHERE "MigrationId" = '20260518001039_InitialCreate'
            );
            """);
    }
}
