using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RPG_ESI07.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuditLog_EventType",
                table: "AuditLogs");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Player");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuditLog_EventType",
                table: "AuditLogs",
                sql: "\"EventType\" IN ('LOGIN_SUCCESS', 'LOGIN_FAILED', 'LOGOUT',\r\n                    'DATA_EXPORT', 'DATA_DELETE', 'DATA_MODIFY',\r\n                    'CHEAT_DETECTED', 'ADMIN_ACTION', 'MFA_ENABLED', 'MFA_FAILED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuditLog_EventType",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuditLog_EventType",
                table: "AuditLogs",
                sql: "\"EventType\" IN ('LOGIN_SUCCESS', 'LOGIN_FAILED', 'LOGOUT', \r\n                    'DATA_EXPORT', 'DATA_DELETE', 'DATA_MODIFY', \r\n                    'CHEAT_DETECTED', 'ADMIN_ACTION', 'MFA_ENABLED', 'MFA_FAILED')");
        }
    }
}