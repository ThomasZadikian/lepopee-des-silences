using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// HAND-AUTHORED REFERENCE MIGRATION — this environment has no dotnet SDK, so this
    /// file (and any paired Designer.cs/ModelSnapshot.cs) could not be generated or
    /// verified by the EF tooling. It is a purely additive nullable column, so no data
    /// backfill is needed: existing rows simply read as NULL (Room.CurrentGridNodeId
    /// is a nullable field on the domain side too, and is only ever read while a grid
    /// room is mid-interaction, so a stale/absent value on old rows is harmless — the
    /// next EnterNodeAtPartyPosition/ChallengeBossRemotely call sets it correctly).
    ///
    /// Before merging, run locally:
    ///   dotnet ef migrations add AddCurrentGridNodeIdToRoom
    /// and use this file's Up()/Down() only as a cross-check against what the tool
    /// generates (it will also regenerate a correct Designer.cs + ModelSnapshot.cs,
    /// which must NOT be hand-written).
    /// </remarks>
    public partial class AddCurrentGridNodeIdToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "current_grid_node_id",
                table: "run_rooms",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_grid_node_id",
                table: "run_rooms");
        }
    }
}
