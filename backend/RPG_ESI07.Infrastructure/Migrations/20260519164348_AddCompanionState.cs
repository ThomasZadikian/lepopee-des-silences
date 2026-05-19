using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RPG_ESI07.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanionStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrentState = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "REPOS"),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VictoriesToday = table.Column<int>(type: "integer", nullable: false),
                    DefeatesToday = table.Column<int>(type: "integer", nullable: false),
                    LastCombatAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionStates", x => x.Id);
                    table.CheckConstraint("CK_CompanionState_State", "\"CurrentState\" IN ('REPOS', 'JEU', 'MANGER', 'EXCITE', 'TRISTE', 'ENDORMI')");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanionStates");
        }
    }
}
