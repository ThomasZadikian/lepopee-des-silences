using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcRoomBindingOfferingsAndReputationAffinity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bound_room_keys_json",
                table: "catalog_npc_definitions",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "offerings_json",
                table: "catalog_npc_definitions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "catalog_npc_reputation_affinities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    npc_key_from = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    npc_key_to = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_npc_reputation_affinities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_npc_reputation_affinities_npc_key_from_npc_key_to",
                table: "catalog_npc_reputation_affinities",
                columns: new[] { "npc_key_from", "npc_key_to" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_npc_reputation_affinities");

            migrationBuilder.DropColumn(
                name: "bound_room_keys_json",
                table: "catalog_npc_definitions");

            migrationBuilder.DropColumn(
                name: "offerings_json",
                table: "catalog_npc_definitions");
        }
    }
}
