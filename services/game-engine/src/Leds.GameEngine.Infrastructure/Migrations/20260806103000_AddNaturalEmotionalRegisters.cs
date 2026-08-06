using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260806103000_AddNaturalEmotionalRegisters")]
public sealed class AddNaturalEmotionalRegisters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "natural_emotional_register",
            table: "run_combatants",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Neutral");

        migrationBuilder.AddColumn<string>(
            name: "emotional_register_code",
            table: "run_character_snapshots",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "neutral");

        migrationBuilder.Sql(
            """
            UPDATE run_character_snapshots
            SET emotional_register_code = CASE LOWER(definition_key)
                WHEN 'character.player.self' THEN 'memoire'
                WHEN 'character.thomas' THEN 'silence'
                WHEN 'character.mane' THEN 'rupture'
                WHEN 'character.mina' THEN 'folie'
                WHEN 'character.elise' THEN 'melancolie'
                WHEN 'character.john' THEN 'deni'
                ELSE emotional_register_code
            END;
            """);

        // One-time compatibility backfill for combats created before natural registers
        // were persisted. New combats never use source keys or archetypes for typing.
        migrationBuilder.Sql(
            """
            UPDATE run_combatants
            SET natural_emotional_register = CASE
                WHEN LOWER(source_key) IN ('player.self', 'character.player.self') THEN 'Memoire'
                WHEN LOWER(source_key) = 'character.thomas' THEN 'Silence'
                WHEN LOWER(source_key) = 'character.mane' THEN 'Rupture'
                WHEN LOWER(source_key) = 'character.mina' THEN 'Folie'
                WHEN LOWER(source_key) = 'character.elise' THEN 'Melancolie'
                WHEN LOWER(source_key) = 'character.john' THEN 'Deni'
                WHEN LOWER(source_key) = 'canon.enemy.grand-cardinal' THEN 'Deni'
                WHEN LOWER(source_key) = 'canon.enemy.imperatrice-vipere' THEN 'Folie'
                WHEN LOWER(source_key) = 'canon.enemy.homoncule-roi' THEN 'Rupture'
                WHEN LOWER(source_key) = 'canon.enemy.pape-louis-xvii' THEN 'Effroi'
                WHEN LOWER(source_key) = 'canon.enemy.himlit' THEN 'Folie'
                WHEN LOWER(archetype) = 'fragile' THEN 'Melancolie'
                WHEN LOWER(archetype) = 'shadow' THEN 'Effroi'
                WHEN LOWER(archetype) IN ('guard', 'bruiser', 'rupture') THEN 'Rupture'
                WHEN LOWER(archetype) = 'memory' THEN 'Memoire'
                WHEN LOWER(archetype) = 'support' THEN 'Deni'
                WHEN LOWER(archetype) = 'disruptor' THEN 'Folie'
                WHEN LOWER(archetype) = 'skirmisher' THEN 'Silence'
                ELSE natural_emotional_register
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "natural_emotional_register",
            table: "run_combatants");

        migrationBuilder.DropColumn(
            name: "emotional_register_code",
            table: "run_character_snapshots");
    }
}
