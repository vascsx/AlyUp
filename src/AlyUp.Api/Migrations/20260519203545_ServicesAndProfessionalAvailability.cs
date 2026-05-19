using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlyUp.Api.Migrations
{
    /// <inheritdoc />
    public partial class ServicesAndProfessionalAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "services",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "services",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "professional_availabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    professional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    salon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_professional_availabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_professional_availabilities_salons_salon_id",
                        column: x => x.salon_id,
                        principalTable: "salons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_professional_availabilities_users_professional_id",
                        column: x => x.professional_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_professional_availabilities_professional_id_day_of_week",
                table: "professional_availabilities",
                columns: new[] { "professional_id", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_professional_availabilities_salon_id",
                table: "professional_availabilities",
                column: "salon_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "professional_availabilities");

            migrationBuilder.DropColumn(
                name: "description",
                table: "services");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "services");
        }
    }
}
