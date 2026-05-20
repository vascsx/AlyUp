using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlyUp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentProfessional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'professionals'
                          AND column_name = 'document'
                    ) THEN
                        ALTER TABLE professionals
                        ADD COLUMN document character varying(20) NOT NULL DEFAULT '';
                    END IF;
                END $$;
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
                          AND table_name = 'professionals'
                          AND column_name = 'document'
                    ) THEN
                        ALTER TABLE professionals
                        DROP COLUMN document;
                    END IF;
                END $$;
                """);
        }
    }
}
