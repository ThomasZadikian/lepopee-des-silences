using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlayerDbContext))]
[Migration("20260922090000_AssignLegacyPermanentItemsToCharacters")]
public partial class AssignLegacyPermanentItemsToCharacters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Historical permanent rewards were stored at profile level only. Their original
        // character cannot be reconstructed, so preserve them by assigning every orphaned
        // instance to the oldest active playable character of the same profile.
        migrationBuilder.Sql(
            """
            INSERT INTO player_character_items
                (id, player_character_id, item_definition_key, acquired_at_utc, source, equipment_position)
            SELECT
                permanent_item.id,
                owner.id,
                permanent_item.item_definition_key,
                permanent_item.acquired_at_utc,
                'legacy-account-inventory',
                NULL
            FROM player_permanent_items AS permanent_item
            JOIN LATERAL (
                SELECT pc.id
                FROM player_characters AS pc
                WHERE pc.player_profile_id = permanent_item.player_profile_id
                ORDER BY
                    CASE
                        WHEN pc.character_type = 'Player' THEN 0
                        WHEN pc.character_type <> 'Companion' THEN 1
                        ELSE 2
                    END,
                    CASE WHEN pc.status = 'Active' THEN 0 ELSE 1 END,
                    pc.created_at_utc,
                    pc.id
                LIMIT 1
            ) AS owner ON TRUE
            WHERE NOT EXISTS (
                SELECT 1
                FROM player_character_items AS assigned
                WHERE assigned.id = permanent_item.id
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DELETE FROM player_character_items WHERE source = 'legacy-account-inventory';");
    }
}
