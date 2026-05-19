using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlyUp.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenHashing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

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

                        IF EXISTS (
                            SELECT 1
                            FROM pg_indexes
                            WHERE schemaname = 'public'
                              AND tablename = 'refresh_tokens'
                              AND indexname = 'ix_refresh_tokens_token'
                        ) THEN
                            ALTER INDEX "ix_refresh_tokens_token" RENAME TO "ix_refresh_tokens_token_hash";
                        END IF;

                        UPDATE refresh_tokens
                        SET token_hash = encode(digest(token_hash, 'sha256'), 'base64');
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'refresh_tokens'
                          AND column_name = 'family_id'
                    ) THEN
                        ALTER TABLE "refresh_tokens" ADD COLUMN "family_id" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'refresh_tokens'
                          AND column_name = 'session_id'
                    ) THEN
                        ALTER TABLE "refresh_tokens" ADD COLUMN "session_id" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                UPDATE refresh_tokens
                SET family_id = CASE WHEN family_id = '00000000-0000-0000-0000-000000000000' THEN gen_random_uuid() ELSE family_id END,
                    session_id = CASE WHEN session_id = '00000000-0000-0000-0000-000000000000' THEN gen_random_uuid() ELSE session_id END;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "users"
                ALTER COLUMN "is_active" DROP DEFAULT;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "ix_refresh_tokens_family_id" ON "refresh_tokens" ("family_id");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "ix_refresh_tokens_session_id" ON "refresh_tokens" ("session_id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "ix_refresh_tokens_family_id";
                DROP INDEX IF EXISTS "ix_refresh_tokens_session_id";
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
                          AND column_name = 'family_id'
                    ) THEN
                        ALTER TABLE "refresh_tokens" DROP COLUMN "family_id";
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'refresh_tokens'
                          AND column_name = 'session_id'
                    ) THEN
                        ALTER TABLE "refresh_tokens" DROP COLUMN "session_id";
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'refresh_tokens'
                          AND column_name = 'token_hash'
                    ) THEN
                        ALTER TABLE "refresh_tokens" RENAME COLUMN "token_hash" TO "token";
                    END IF;

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

            migrationBuilder.Sql(
                """
                ALTER TABLE "users"
                ALTER COLUMN "is_active" SET DEFAULT TRUE;
                """);
        }
    }
}
