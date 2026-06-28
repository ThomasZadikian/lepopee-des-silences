using Leds.Player.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Leds.Player.IntegrationTests;

public sealed class PlayerPostgresFixture : IAsyncLifetime
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

    public (PlayerDbContext Context, string ConnectionString) CreateContext()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var connStr = CreateDatabase(_container.GetConnectionString(), dbName);
        var options = new DbContextOptionsBuilder<PlayerDbContext>()
            .UseNpgsql(connStr)
            .Options;
        var context = new PlayerDbContext(options);
        context.Database.Migrate();
        return (context, connStr);
    }

    public PlayerDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PlayerDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new PlayerDbContext(options);
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

[CollectionDefinition("PlayerPostgres")]
public class PlayerPostgresCollection : ICollectionFixture<PlayerPostgresFixture>;
