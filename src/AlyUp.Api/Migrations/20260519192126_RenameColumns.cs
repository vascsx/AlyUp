using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlyUp.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'users'
                          AND column_name = 'password'
                    ) THEN
                        ALTER TABLE "users" RENAME COLUMN "password" TO "password_hash";
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'refresh_tokens'
                          AND column_name = 'token'
                    ) THEN
                        ALTER TABLE "refresh_tokens" RENAME COLUMN "token" TO "token_hash";
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'public'
                          AND tablename = 'refresh_tokens'
                          AND indexname = 'ix_refresh_tokens_token'
                    ) THEN
                        ALTER INDEX "ix_refresh_tokens_token" RENAME TO "ix_refresh_tokens_token_hash";
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'users'
                          AND column_name = 'password_hash'
                    ) THEN
                        ALTER TABLE "users" RENAME COLUMN "password_hash" TO "password";
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'refresh_tokens'
                          AND column_name = 'token_hash'
                    ) THEN
                        ALTER TABLE "refresh_tokens" RENAME COLUMN "token_hash" TO "token";
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'public'
                          AND tablename = 'refresh_tokens'
                          AND indexname = 'ix_refresh_tokens_token_hash'
                    ) THEN
                        ALTER INDEX "ix_refresh_tokens_token_hash" RENAME TO "ix_refresh_tokens_token";
                    END IF;
                END
                $$;
                """);
        }
    }
}
