using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRunMapStatePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "current_depth",
                table: "runs",
                newName: "speed");

            migrationBuilder.AddColumn<int>(
                name: "attack",
                table: "runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "current_hp",
                table: "runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "current_room_id",
                table: "runs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "current_room_index",
                table: "runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "defense",
                table: "runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ended_at_utc",
                table: "runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_hp",
                table: "runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "pre_suspend_status",
                table: "runs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "saved_at_utc",
                table: "runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_active_palace_laws",
                table: "runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "snapshot_attack",
                table: "runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "snapshot_current_hp",
                table: "runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "snapshot_defense",
                table: "runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_memory_fragments",
                table: "runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "snapshot_speed",
                table: "runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "started_at_utc",
                table: "runs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "run_active_palace_laws",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    law_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    domains = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_active_palace_laws", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_active_palace_laws_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_memory_fragments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fragment_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_memory_fragments", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_memory_fragments_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    room_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    theme = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    boss_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    boss_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    boss_room_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    boss_danger_hint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    boss_enemy_template_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    current_node_depth = table.Column<int>(type: "integer", nullable: false),
                    max_node_depth = table.Column<int>(type: "integer", nullable: false),
                    layout_template_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    layout_template_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_rooms", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_rooms_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    lane = table.Column<int>(type: "integer", nullable: false),
                    risk_level = table.Column<int>(type: "integer", nullable: false),
                    reward_profile = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_boss = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    chosen_event_option_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_nodes", x => x.id);
                    table.ForeignKey(
                        name: "FK_run_nodes_run_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "run_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_node_parent_nodes",
                columns: table => new
                {
                    map_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_node_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_node_parent_nodes", x => new { x.map_node_id, x.parent_node_id });
                    table.ForeignKey(
                        name: "FK_run_node_parent_nodes_run_nodes_map_node_id",
                        column: x => x.map_node_id,
                        principalTable: "run_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_run_active_palace_laws_run_id",
                table: "run_active_palace_laws",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_memory_fragments_run_id",
                table: "run_memory_fragments",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_node_parent_nodes_map_node_id",
                table: "run_node_parent_nodes",
                column: "map_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_node_parent_nodes_parent_node_id",
                table: "run_node_parent_nodes",
                column: "parent_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_nodes_room_id",
                table: "run_nodes",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_nodes_row",
                table: "run_nodes",
                column: "row");

            migrationBuilder.CreateIndex(
                name: "IX_run_nodes_state",
                table: "run_nodes",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "IX_run_rooms_run_id",
                table: "run_rooms",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_run_rooms_state",
                table: "run_rooms",
                column: "state");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "run_active_palace_laws");

            migrationBuilder.DropTable(
                name: "run_memory_fragments");

            migrationBuilder.DropTable(
                name: "run_node_parent_nodes");

            migrationBuilder.DropTable(
                name: "run_nodes");

            migrationBuilder.DropTable(
                name: "run_rooms");

            migrationBuilder.DropColumn(
                name: "attack",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "current_hp",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "current_room_id",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "current_room_index",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "defense",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "ended_at_utc",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "max_hp",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "pre_suspend_status",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "saved_at_utc",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "snapshot_active_palace_laws",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "snapshot_attack",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "snapshot_current_hp",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "snapshot_defense",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "snapshot_memory_fragments",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "snapshot_speed",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "started_at_utc",
                table: "runs");

            migrationBuilder.RenameColumn(
                name: "speed",
                table: "runs",
                newName: "current_depth");
        }
    }
}