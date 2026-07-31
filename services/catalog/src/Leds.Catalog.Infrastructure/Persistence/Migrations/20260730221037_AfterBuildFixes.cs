using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leds.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AfterBuildFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: columns already exist in the database from earlier tactical migrations.
            // This migration exists solely to synchronize the EF model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: columns were not added by this migration.
        }
    }
}
