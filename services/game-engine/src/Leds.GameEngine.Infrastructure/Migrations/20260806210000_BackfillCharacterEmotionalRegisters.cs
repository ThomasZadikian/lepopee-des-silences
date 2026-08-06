using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.GameEngine.Infrastructure.Migrations;

[DbContext(typeof(GameEngineDbContext))]
[Migration("20260806210000_BackfillCharacterEmotionalRegisters")]
public sealed class BackfillCharacterEmotionalRegisters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // One-time repair for snapshots written between AddNaturalEmotionalRegisters and
        // the persistence-mapper fix. These values mirror the Catalog definitions that
        // existed when the invalid snapshots were created; runtime code never uses this
        // migration as an authored-content source.
        migrationBuilder.Sql(
            """
            UPDATE run_character_snapshots
            SET emotional_register_code = CASE lower(definition_key)
                WHEN 'character.player.self' THEN 'memoire'
                WHEN 'character.thomas' THEN 'silence'
                WHEN 'character.mane' THEN 'rupture'
                WHEN 'character.mina' THEN 'folie'
                WHEN 'character.elise' THEN 'melancolie'
                WHEN 'character.john' THEN 'deni'
                ELSE emotional_register_code
            END
            WHERE emotional_register_code IS NULL OR btrim(emotional_register_code) = '';

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM run_character_snapshots
                    WHERE emotional_register_code IS NULL OR btrim(emotional_register_code) = ''
                ) THEN
                    RAISE EXCEPTION
                        'Cannot backfill emotional register for one or more unknown character definition keys.';
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data repair is intentionally irreversible: clearing valid authored registers
        // would recreate corrupted run snapshots.
    }
}
