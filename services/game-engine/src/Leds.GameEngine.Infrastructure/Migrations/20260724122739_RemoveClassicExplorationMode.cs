using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClassicExplorationMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "exploration_mode",
                table: "runs");

            migrationBuilder.AlterColumn<string>(
                name: "grid_revealed_node_ids_csv",
                table: "run_rooms",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "grid_revealed_cells_csv",
                table: "run_rooms",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "grid_width",
                table: "run_rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "grid_start_y",
                table: "run_rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "grid_start_x",
                table: "run_rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "grid_party_y",
                table: "run_rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "grid_party_x",
                table: "run_rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "grid_movement_budget_remaining",
                table: "run_rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "grid_movement_budget",
                table: "run_rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "grid_height",
                table: "run_rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "exploration_mode",
                table: "runs",
                type: "text",
                nullable: false,
                defaultValue: "Classic");

            migrationBuilder.AlterColumn<string>(
                name: "grid_revealed_node_ids_csv",
                table: "run_rooms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "grid_revealed_cells_csv",
                table: "run_rooms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "grid_width",
                table: "run_rooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "grid_start_y",
                table: "run_rooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "grid_start_x",
                table: "run_rooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "grid_party_y",
                table: "run_rooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "grid_party_x",
                table: "run_rooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "grid_movement_budget_remaining",
                table: "run_rooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "grid_movement_budget",
                table: "run_rooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "grid_height",
                table: "run_rooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
