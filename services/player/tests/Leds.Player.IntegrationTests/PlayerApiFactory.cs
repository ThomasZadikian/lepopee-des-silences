using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Players;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Leds.Player.IntegrationTests;

public sealed class PlayerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string GameClientOrigin = "http://localhost:5173";

    private PostgreSqlContainer? _container;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16")
            .Build();

        await _container.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        Dispose();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlayerDb"] = GetConnectionString(),
                ["Cors:AllowedOrigins:0"] = GameClientOrigin
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IArchetypeDefinitionGateway>();
            services.AddSingleton<IArchetypeDefinitionGateway, TestArchetypeGateway>();
        });
    }

    private string GetConnectionString()
    {
        return _container?.GetConnectionString()
            ?? throw new InvalidOperationException("The PostgreSQL test container has not been started.");
    }

    private sealed class TestArchetypeGateway : IArchetypeDefinitionGateway
    {
        public Task<ArchetypeDefinitionSnapshot?> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<ArchetypeDefinitionSnapshot?>(
                string.Equals(key, "archetype.porteur", StringComparison.OrdinalIgnoreCase)
                    ? new ArchetypeDefinitionSnapshot(
                        "archetype.porteur", PlayerCharacterStatBlock.CreateDefaultPorteur(), [], [],
                        ["skill.basic.guard"], ["skill.basic.guard"])
                    : null);
    }
}

[CollectionDefinition("PlayerApi")]
public sealed class PlayerApiCollection : ICollectionFixture<PlayerApiFactory>;
