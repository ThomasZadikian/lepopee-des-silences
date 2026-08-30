using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Leds.Player.Infrastructure.Persistence;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlayerDbContext))]
[Migration("20260824160000_AddMainStoryProgress")]
public partial class AddMainStoryProgress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("main_story_sequence_key", "player_profiles", type: "character varying(160)", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<string>("main_story_sequence_version", "player_profiles", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>("main_story_step_key", "player_profiles", type: "character varying(160)", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<string>("main_story_checkpoint_key", "player_profiles", type: "character varying(160)", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<bool>("main_story_completed", "player_profiles", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>("highest_difficulty_level_unlocked", "player_profiles", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>("main_story_unlocked_room_keys_json", "player_profiles", type: "text", nullable: false, defaultValue: "[]");
        migrationBuilder.AddColumn<string>("main_story_visible_room_keys_json", "player_profiles", type: "text", nullable: false, defaultValue: "[]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("main_story_sequence_key", "player_profiles");
        migrationBuilder.DropColumn("main_story_sequence_version", "player_profiles");
        migrationBuilder.DropColumn("main_story_step_key", "player_profiles");
        migrationBuilder.DropColumn("main_story_checkpoint_key", "player_profiles");
        migrationBuilder.DropColumn("main_story_completed", "player_profiles");
        migrationBuilder.DropColumn("highest_difficulty_level_unlocked", "player_profiles");
        migrationBuilder.DropColumn("main_story_unlocked_room_keys_json", "player_profiles");
        migrationBuilder.DropColumn("main_story_visible_room_keys_json", "player_profiles");
    }
}
