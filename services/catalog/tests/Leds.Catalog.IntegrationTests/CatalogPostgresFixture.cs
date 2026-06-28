using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Leds.Catalog.IntegrationTests;

public sealed class CatalogPostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .Build();
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public (CatalogDbContext Context, string ConnectionString) CreateContext()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var connStr = CreateDatabase(_container.GetConnectionString(), dbName);
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connStr)
            .Options;
        var context = new CatalogDbContext(options);
        context.Database.Migrate();
        return (context, connStr);
    }

    public CatalogDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new CatalogDbContext(options);
    }

    private static string CreateDatabase(string baseConnStr, string dbName)
    {
        using var conn = new NpgsqlConnection(baseConnStr);
        conn.Open();
        using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn);
        cmd.ExecuteNonQuery();
        var builder = new NpgsqlConnectionStringBuilder(baseConnStr) { Database = dbName };
        return builder.ConnectionString;
    }
}

[CollectionDefinition("CatalogPostgres")]
public class CatalogPostgresCollection : ICollectionFixture<CatalogPostgresFixture>;
