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
    /// backfill is needed: existing rows simply read as NULL (combat nodes will get a
    /// real tier again the next time they're generated).
    ///
    /// Before merging, run locally:
    ///   dotnet ef migrations add AddCombatRiskTierToNode
    /// and use this file's Up()/Down() only as a cross-check against what the tool
    /// generates (it will also regenerate a correct Designer.cs + ModelSnapshot.cs,
    /// which must NOT be hand-written).
    /// </remarks>
    public partial class AddCombatRiskTierToNode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "combat_risk_tier",
                table: "run_nodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "combat_risk_tier",
                table: "run_nodes");
        }
    }
}
