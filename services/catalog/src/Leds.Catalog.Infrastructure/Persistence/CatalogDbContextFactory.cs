using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Leds.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5434;Database=leds_catalog;Username=postgres;Password=postgres";

    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();

        // Design-time connection string. Allow an env override so the local port
        // (5434) is not hardcoded; falls back to the documented dev default.
        var connectionString =
            Environment.GetEnvironmentVariable("CATALOG_DB_CONNECTION_STRING")
            ?? DefaultConnectionString;

        optionsBuilder.UseNpgsql(connectionString);

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
