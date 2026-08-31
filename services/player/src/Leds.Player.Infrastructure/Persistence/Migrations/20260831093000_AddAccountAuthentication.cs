using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Player.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlayerDbContext))]
[Migration("20260831093000_AddAccountAuthentication")]
public partial class AddAccountAuthentication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_player_characters_player_profile_id_definition_key",
            table: "player_characters");

        migrationBuilder.AddColumn<string>(
            name: "archetype_key",
            table: "player_characters",
            type: "character varying(160)",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "archived_at_utc",
            table: "player_characters",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_player_characters_player_profile_id_definition_key",
            table: "player_characters",
            columns: new[] { "player_profile_id", "definition_key" });

        migrationBuilder.CreateTable(
            name: "account_identities",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                password_hash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                role = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                email_verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                mfa_secret_protected = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                mfa_configured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                recovery_code_hashes_json = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                closure_requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                closure_execute_after_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                closure_cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_account_identities", x => x.id);
                table.ForeignKey(
                    name: "FK_account_identities_player_profiles_account_id",
                    column: x => x.account_id,
                    principalTable: "player_profiles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "account_sessions",
            columns: table => new
            {
                session_id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                rotated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_account_sessions", x => x.session_id);
                table.ForeignKey(
                    name: "FK_account_sessions_player_profiles_account_id",
                    column: x => x.account_id,
                    principalTable: "player_profiles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "account_security_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_account_security_tokens", x => x.id);
                table.ForeignKey(
                    name: "FK_account_security_tokens_player_profiles_account_id",
                    column: x => x.account_id,
                    principalTable: "player_profiles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "account_privacy_consents",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                purpose_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                policy_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_account_privacy_consents", x => x.id);
                table.ForeignKey(
                    name: "FK_account_privacy_consents_player_profiles_account_id",
                    column: x => x.account_id,
                    principalTable: "player_profiles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "active_game_session_leases",
            columns: table => new
            {
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                acquired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_active_game_session_leases", x => x.account_id);
                table.ForeignKey(
                    name: "FK_active_game_session_leases_player_profiles_account_id",
                    column: x => x.account_id,
                    principalTable: "player_profiles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_account_identities_account_id",
            table: "account_identities",
            column: "account_id",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_account_identities_email",
            table: "account_identities",
            column: "email",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_account_sessions_account_id",
            table: "account_sessions",
            column: "account_id");
        migrationBuilder.CreateIndex(
            name: "IX_account_security_tokens_account_id",
            table: "account_security_tokens",
            column: "account_id");
        migrationBuilder.CreateIndex(
            name: "IX_account_security_tokens_purpose_token_hash",
            table: "account_security_tokens",
            columns: new[] { "purpose", "token_hash" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_account_privacy_consents_account_id_purpose_key_granted_at_utc",
            table: "account_privacy_consents",
            columns: new[] { "account_id", "purpose_key", "granted_at_utc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "active_game_session_leases");
        migrationBuilder.DropTable(name: "account_privacy_consents");
        migrationBuilder.DropTable(name: "account_security_tokens");
        migrationBuilder.DropTable(name: "account_sessions");
        migrationBuilder.DropTable(name: "account_identities");

        migrationBuilder.DropIndex(
            name: "IX_player_characters_player_profile_id_definition_key",
            table: "player_characters");
        migrationBuilder.DropColumn(name: "archetype_key", table: "player_characters");
        migrationBuilder.DropColumn(name: "archived_at_utc", table: "player_characters");
        migrationBuilder.CreateIndex(
            name: "IX_player_characters_player_profile_id_definition_key",
            table: "player_characters",
            columns: new[] { "player_profile_id", "definition_key" },
            unique: true);
    }
}
