using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GameEngineDbContext))]
    [Migration("20260701093000_AddRewardOptionLootSource")]
    public partial class AddRewardOptionLootSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "source_enemy_key",
                table: "run_reward_options",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_enemy_display_name",
                table: "run_reward_options",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source_enemy_key",
                table: "run_reward_options");

            migrationBuilder.DropColumn(
                name: "source_enemy_display_name",
                table: "run_reward_options");
        }
    }
}
