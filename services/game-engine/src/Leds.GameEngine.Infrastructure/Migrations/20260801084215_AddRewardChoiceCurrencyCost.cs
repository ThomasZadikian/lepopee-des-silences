using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardChoiceCurrencyCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "him_lit_shard_cost",
                table: "run_reward_options",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "palace_shard_cost",
                table: "run_reward_options",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "him_lit_shard_cost",
                table: "run_reward_options");

            migrationBuilder.DropColumn(
                name: "palace_shard_cost",
                table: "run_reward_options");
        }
    }
}
