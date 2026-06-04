using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RPG_ESI07.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGameSaveZoneConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GameSave_Zone",
                table: "GameSaves");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_GameSave_Zone",
                table: "GameSaves",
                sql: "\"CurrentZone\" IN ('Tutorial', 'BossFinal')");
        }
    }
}