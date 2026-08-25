using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260824150000_AlignRunLifecycleAndConcurrency")]
public partial class AlignRunLifecycleAndConcurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "outcome",
            table: "runs",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "revision",
            table: "runs",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "technical_recovery_state",
            table: "runs",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "None");

        migrationBuilder.Sql("""
            UPDATE runs
            SET outcome = CASE status
                    WHEN 'Completed' THEN 'Success'
                    WHEN 'Failed' THEN 'Defeat'
                    WHEN 'Abandoned' THEN 'Abandon'
                    ELSE outcome
                END,
                status = CASE status
                    WHEN 'Completed' THEN 'Resolved'
                    WHEN 'Failed' THEN 'Resolved'
                    WHEN 'Abandoned' THEN 'Resolved'
                    WHEN 'Created' THEN 'Active'
                    WHEN 'RoomResolved' THEN 'Active'
                    WHEN 'BossReached' THEN 'Active'
                    WHEN 'Interlude' THEN 'Active'
                    ELSE status
                END;
            """);

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT player_id
                    FROM runs
                    WHERE status IN ('Active', 'Suspended')
                    GROUP BY player_id
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Cannot enforce one open Run per Account: duplicate Active/Suspended runs exist.';
                END IF;
            END $$;
            """);

        migrationBuilder.CreateIndex(
            name: "ux_runs_player_active_or_suspended",
            table: "runs",
            column: "player_id",
            unique: true,
            filter: "status IN ('Active', 'Suspended')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_runs_lifecycle_outcome",
            table: "runs",
            sql: "(status = 'Resolved' AND outcome IS NOT NULL) OR (status IN ('Active', 'Suspended') AND outcome IS NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_runs_lifecycle_outcome",
            table: "runs");

        migrationBuilder.DropIndex(
            name: "ux_runs_player_active_or_suspended",
            table: "runs");

        migrationBuilder.Sql("""
            UPDATE runs
            SET status = CASE outcome
                    WHEN 'Success' THEN 'Completed'
                    WHEN 'Defeat' THEN 'Failed'
                    WHEN 'Abandon' THEN 'Abandoned'
                    ELSE status
                END
            WHERE status = 'Resolved';
            """);

        migrationBuilder.DropColumn(name: "outcome", table: "runs");
        migrationBuilder.DropColumn(name: "revision", table: "runs");
        migrationBuilder.DropColumn(name: "technical_recovery_state", table: "runs");
    }
}
