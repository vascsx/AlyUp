using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlyUp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AuditDatesToUtcMinus3 : Migration
    {
        private const string AppTimeZoneOffset = "'-03:00'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AlterAuditColumnToAppLocal(migrationBuilder, "users", "updated_at");
            AlterAuditColumnToAppLocal(migrationBuilder, "users", "created_at");
            AlterAuditColumnToAppLocal(migrationBuilder, "services", "created_at");
            AlterAuditColumnToAppLocal(migrationBuilder, "salons", "updated_at");
            AlterAuditColumnToAppLocal(migrationBuilder, "salons", "created_at");
            AlterAuditColumnToAppLocal(migrationBuilder, "clients", "updated_at");
            AlterAuditColumnToAppLocal(migrationBuilder, "clients", "created_at");
            AlterAuditColumnToAppLocal(migrationBuilder, "appointments", "updated_at");
            AlterAuditColumnToAppLocal(migrationBuilder, "appointments", "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AlterAuditColumnToUtcOffset(migrationBuilder, "users", "updated_at");
            AlterAuditColumnToUtcOffset(migrationBuilder, "users", "created_at");
            AlterAuditColumnToUtcOffset(migrationBuilder, "services", "created_at");
            AlterAuditColumnToUtcOffset(migrationBuilder, "salons", "updated_at");
            AlterAuditColumnToUtcOffset(migrationBuilder, "salons", "created_at");
            AlterAuditColumnToUtcOffset(migrationBuilder, "clients", "updated_at");
            AlterAuditColumnToUtcOffset(migrationBuilder, "clients", "created_at");
            AlterAuditColumnToUtcOffset(migrationBuilder, "appointments", "updated_at");
            AlterAuditColumnToUtcOffset(migrationBuilder, "appointments", "created_at");
        }

        private static void AlterAuditColumnToAppLocal(MigrationBuilder migrationBuilder, string table, string column)
        {
            migrationBuilder.Sql(
                $"""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '{table}'
                          AND column_name = '{column}'
                          AND data_type = 'timestamp with time zone'
                    ) THEN
                        ALTER TABLE "{table}"
                        ALTER COLUMN "{column}" TYPE timestamp without time zone
                        USING "{column}" AT TIME ZONE {AppTimeZoneOffset};
                    END IF;
                END
                $$;
                """);
        }

        private static void AlterAuditColumnToUtcOffset(MigrationBuilder migrationBuilder, string table, string column)
        {
            migrationBuilder.Sql(
                $"""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '{table}'
                          AND column_name = '{column}'
                          AND data_type = 'timestamp without time zone'
                    ) THEN
                        ALTER TABLE "{table}"
                        ALTER COLUMN "{column}" TYPE timestamp with time zone
                        USING "{column}" AT TIME ZONE {AppTimeZoneOffset};
                    END IF;
                END
                $$;
                """);
        }
    }
}
