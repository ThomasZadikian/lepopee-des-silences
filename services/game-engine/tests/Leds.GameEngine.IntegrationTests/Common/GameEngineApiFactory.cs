using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.UnitTests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Leds.GameEngine.IntegrationTests.Common;

public sealed class GameEngineApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private readonly SemaphoreSlim _resetLock = new(1, 1);

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .Build();

        await _container.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        Dispose();
        _resetLock.Dispose();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public new HttpClient CreateClient()
    {
        ResetDatabase();
        return base.CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GameEngineDb"] = GetConnectionString(),
                ["CatalogGateway:BaseUrl"] = "http://catalog.test",
                ["PlayerGateway:BaseUrl"] = "http://player.test",
                ["Outbox:DispatcherEnabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICatalogContentGateway>();
            services.RemoveAll<IPlayerRunSnapshotGateway>();
            services.RemoveAll<IPlayerProfileGateway>();

            services.AddSingleton<ICatalogContentGateway, StubCatalogContentGateway>();
            services.AddSingleton<IPlayerRunSnapshotGateway, TestPlayerRunSnapshotGateway>();
            services.AddSingleton<IPlayerProfileGateway, StubPlayerProfileGateway>();
        });
    }

    private string GetConnectionString()
    {
        if (_container is null)
        {
            throw new InvalidOperationException("The PostgreSQL test container has not been started.");
        }

        return _container.GetConnectionString();
    }

    private void ResetDatabase()
    {
        _resetLock.Wait();

        try
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GameEngineDbContext>();
            dbContext.Database.Migrate();
            dbContext.Database.ExecuteSqlRaw(
                """
                DO $$
                DECLARE table_record RECORD;
                BEGIN
                    FOR table_record IN
                        SELECT tablename
                        FROM pg_tables
                        WHERE schemaname = 'public'
                          AND tablename <> '__EFMigrationsHistory'
                    LOOP
                        EXECUTE 'TRUNCATE TABLE public.' || quote_ident(table_record.tablename) || ' CASCADE';
                    END LOOP;
                END $$;
                """);
        }
        finally
        {
            _resetLock.Release();
        }
    }
}

public sealed class TestPlayerRunSnapshotGateway : IPlayerRunSnapshotGateway
{
    public Task<PlayerRunSnapshot> GetRunSnapshotAsync(
        Guid playerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new PlayerRunSnapshot(
            playerId,
            "Test Player",
            [new PlayerRunSnapshotCharacter(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "character.player.self",
                "Le Porteur",
                new PlayerRunSnapshotCharacterStats(
                    MaxVitality: 100,
                    AttackPower: 12,
                    Defense: 6,
                    StartingGuard: 0,
                    Speed: 10,
                    Initiative: 10,
                    Focus: 0,
                    Mana: 0,
                    Charge: 0),
                [
                    new PlayerRunSnapshotCharacterSkill(
                        SkillDefinitionKey: "skill.basic.strike",
                        DisplayName: "Frappe",
                        SkillType: "Damage",
                        TargetingMode: "SingleEnemy",
                        EffectType: "Damage",
                        ManaCost: 0,
                        ChargeCost: 0,
                        BasePower: 10),
                    new PlayerRunSnapshotCharacterSkill(
                        SkillDefinitionKey: "skill.basic.guard",
                        DisplayName: "Garde",
                        SkillType: "Defense",
                        TargetingMode: "Self",
                        EffectType: "Guard",
                        ManaCost: 0,
                        ChargeCost: 0,
                        BasePower: 5)
                ]]));
    }
}
