using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Leds.Catalog.IntegrationTests;

public sealed class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
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
                ["ConnectionStrings:CatalogDb"] = GetConnectionString(),
                ["CatalogSeed:ApplyOnStartup"] = "true"
            });
        });
    }

    private string GetConnectionString()
    {
        return _container?.GetConnectionString()
            ?? throw new InvalidOperationException("The PostgreSQL test container has not been started.");
    }
}

[CollectionDefinition("CatalogApi")]
public sealed class CatalogApiCollection : ICollectionFixture<CatalogApiFactory>;
