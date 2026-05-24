using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RPG_ESI07.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkovSystemAndNpcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "CurrentZone",
                table: "PlayerProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "ScalingFactor",
                table: "PlayerProfiles",
                type: "real",
                nullable: false,
                defaultValue: 1f);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentZone",
                table: "GameSaves",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "CombatScripts",
                table: "Enemies",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "InfluenceRadius",
                table: "Enemies",
                type: "real",
                nullable: false,
                defaultValue: 5f);

            migrationBuilder.AddColumn<string>(
                name: "InitialState",
                table: "Enemies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "REPOS");

            migrationBuilder.AddColumn<string>(
                name: "MapStates",
                table: "Enemies",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransitionMatrix",
                table: "Enemies",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Npcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SpawnX = table.Column<float>(type: "real", nullable: false),
                    SpawnY = table.Column<float>(type: "real", nullable: false),
                    InfluenceRadius = table.Column<float>(type: "real", nullable: false, defaultValue: 5f),
                    TransitionMatrix = table.Column<string>(type: "jsonb", nullable: true),
                    MapStates = table.Column<string>(type: "jsonb", nullable: true),
                    InitialState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "SEREIN"),
                    Dialogues = table.Column<string>(type: "jsonb", nullable: true),
                    IsMerchant = table.Column<bool>(type: "boolean", nullable: false),
                    MerchantInventory = table.Column<string>(type: "jsonb", nullable: true),
                    Quests = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Npcs", x => x.Id);
                    table.CheckConstraint("CK_Npc_InfluenceRadius", "\"InfluenceRadius\" > 0");
                    table.CheckConstraint("CK_Npc_Type", "\"Type\" IN ('neutral', 'merchant', 'quest', 'ally')");
                });

            migrationBuilder.CreateTable(
                name: "NpcInteractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NpcId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InteractedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcInteractions", x => x.Id);
                    table.CheckConstraint("CK_NpcInteraction_EventType", "\"EventType\" IN ('DIALOGUE', 'TRADE', 'QUEST_START', 'QUEST_COMPLETE')");
                    table.ForeignKey(
                        name: "FK_NpcInteractions_Npcs_NpcId",
                        column: x => x.NpcId,
                        principalTable: "Npcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NpcInteractions_PlayerProfiles_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlayerProfile_ScalingFactor",
                table: "PlayerProfiles",
                sql: "\"ScalingFactor\" >= 1.0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Enemy_InfluenceRadius",
                table: "Enemies",
                sql: "\"InfluenceRadius\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_NpcInteractions_InteractedAt",
                table: "NpcInteractions",
                column: "InteractedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NpcInteractions_NpcId_PlayerId",
                table: "NpcInteractions",
                columns: new[] { "NpcId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_NpcInteractions_PlayerId",
                table: "NpcInteractions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_Name",
                table: "Npcs",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NpcInteractions");

            migrationBuilder.DropTable(
                name: "Npcs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlayerProfile_ScalingFactor",
                table: "PlayerProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Enemy_InfluenceRadius",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "CurrentZone",
                table: "PlayerProfiles");

            migrationBuilder.DropColumn(
                name: "ScalingFactor",
                table: "PlayerProfiles");

            migrationBuilder.DropColumn(
                name: "CombatScripts",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "InfluenceRadius",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "InitialState",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "MapStates",
                table: "Enemies");

            migrationBuilder.DropColumn(
                name: "TransitionMatrix",
                table: "Enemies");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentZone",
                table: "GameSaves",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddCheckConstraint(
                name: "CK_GameSave_Zone",
                table: "GameSaves",
                sql: "\"CurrentZone\" IN ('Tutorial', 'BossFinal')");
        }
    }
}
