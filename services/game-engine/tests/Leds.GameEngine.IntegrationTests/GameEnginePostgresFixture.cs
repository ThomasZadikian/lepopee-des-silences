using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Leds.GameEngine.IntegrationTests;

public sealed class GameEnginePostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16")
            .Build();
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public (GameEngineDbContext Context, string ConnectionString) CreateContext()
    {
        var dbName = $"test_{Guid.NewGuid():N}";
        var connStr = CreateDatabase(_container.GetConnectionString(), dbName);
        var options = new DbContextOptionsBuilder<GameEngineDbContext>()
            .UseNpgsql(connStr)
            .Options;
        var context = new GameEngineDbContext(options);
        context.Database.Migrate();
        return (context, connStr);
    }

    public GameEngineDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<GameEngineDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new GameEngineDbContext(options);
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

[CollectionDefinition("GameEnginePostgres")]
public class GameEnginePostgresCollection : ICollectionFixture<GameEnginePostgresFixture>;
