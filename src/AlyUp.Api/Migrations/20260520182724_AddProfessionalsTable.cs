using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlyUp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_professional_availabilities_users_professional_id",
                table: "professional_availabilities");

            migrationBuilder.CreateTable(
                name: "professionals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    salon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    document = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_professionals", x => x.id);
                    table.ForeignKey(
                        name: "fk_professionals_salons_salon_id",
                        column: x => x.salon_id,
                        principalTable: "salons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_professionals_users_id",
                        column: x => x.id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_professionals_email",
                table: "professionals",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_professionals_salon_id",
                table: "professionals",
                column: "salon_id");

            migrationBuilder.AddForeignKey(
                name: "fk_professional_availabilities_professionals_professional_id",
                table: "professional_availabilities",
                column: "professional_id",
                principalTable: "professionals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_professional_availabilities_professionals_professional_id",
                table: "professional_availabilities");

            migrationBuilder.DropTable(
                name: "professionals");

            migrationBuilder.AddForeignKey(
                name: "fk_professional_availabilities_users_professional_id",
                table: "professional_availabilities",
                column: "professional_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
