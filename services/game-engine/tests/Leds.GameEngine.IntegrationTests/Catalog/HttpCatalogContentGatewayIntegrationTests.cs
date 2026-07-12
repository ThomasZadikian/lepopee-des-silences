extern alias CatalogApi;

using FluentAssertions;
using CatalogApi::Leds.Catalog.Api;
using Leds.Catalog.Infrastructure.Persistence;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Infrastructure.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Leds.GameEngine.IntegrationTests.Catalog;

public sealed class HttpCatalogContentGatewayIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private WebApplicationFactory<CatalogApiAssemblyMarker> _factory = null!;
    private HttpCatalogContentGateway _gateway = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .Build();
        await _container.StartAsync();

        _factory = new WebApplicationFactory<CatalogApiAssemblyMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(_ => { });
                });

                builder.UseSetting(
                    "ConnectionStrings:CatalogDb",
                    _container.GetConnectionString());
            });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await dbContext.Database.MigrateAsync();

        var seedRunner = scope.ServiceProvider.GetRequiredService<CatalogSeedRunner>();
        await seedRunner.SeedAsync();

        var httpClient = _factory.CreateClient();
        _gateway = new HttpCatalogContentGateway(httpClient);
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    // ── Templates ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetEnemyTemplateByKeyAsync_ShouldReturnSeededTemplate()
    {
        var result = await _gateway.GetEnemyTemplateByKeyAsync("enemy-shadow-wolf");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("enemy-shadow-wolf");
        result.Value.Name.Should().Be("Loup d\u2019Ombre");
        result.Value.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetEnemyTemplateByKeyAsync_ShouldFail_ForUnknownKey()
    {
        var result = await _gateway.GetEnemyTemplateByKeyAsync("enemy-nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.enemy_template_not_found");
    }

    [Fact]
    public async Task GetEnemyTemplateByKeyAsync_ShouldFail_ForEmptyKey()
    {
        var result = await _gateway.GetEnemyTemplateByKeyAsync("");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.enemy_template_key_required");
    }

    [Fact]
    public async Task GetSkillTemplateByKeyAsync_ShouldReturnSeededTemplate()
    {
        var result = await _gateway.GetSkillTemplateByKeyAsync("skill-shadow-bite");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("skill-shadow-bite");
        result.Value.Name.Should().Be("Morsure d\u2019Ombre");
        result.Value.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetSkillTemplateByKeyAsync_ShouldFail_ForUnknownKey()
    {
        var result = await _gateway.GetSkillTemplateByKeyAsync("skill-nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.skill_template_not_found");
    }

    [Fact]
    public async Task GetItemTemplateByKeyAsync_ShouldFail_ForUnknownKey()
    {
        // Item templates reuse ItemDefinitionEntity; no dedicated item templates are seeded.
        var result = await _gateway.GetItemTemplateByKeyAsync("item-nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.item_template_not_found");
    }

    [Fact]
    public async Task GetEventTemplateByKeyAsync_ShouldReturnSeededTemplate()
    {
        var result = await _gateway.GetEventTemplateByKeyAsync("event-memory-threshold-v1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("event-memory-threshold-v1");
        result.Value.Name.Should().Be("Mémoire du seuil");
        result.Value.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetEventTemplateByKeyAsync_ShouldFail_ForUnknownKey()
    {
        var result = await _gateway.GetEventTemplateByKeyAsync("event-nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.event_template_not_found");
    }

    // ── Palace Laws ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPalaceLawDefinitionByKeyAsync_ShouldReturnSeededLaw()
    {
        var all = await _gateway.ListActivePalaceLawDefinitionsAsync();
        var first = all.FirstOrDefault();

        first.Should().NotBeNull();

        var result = await _gateway.GetPalaceLawDefinitionByKeyAsync(first!.Key);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be(first.Key);
    }

    [Fact]
    public async Task GetPalaceLawDefinitionByKeyAsync_ShouldFail_ForUnknownKey()
    {
        var result = await _gateway.GetPalaceLawDefinitionByKeyAsync("law-nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.palace_law_definition_not_found");
    }

    // ── Curses ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurseDefinitionByKeyAsync_ShouldReturnSeededCurse()
    {
        var all = await _gateway.ListAvailableCurseDefinitionsAsync();
        var first = all.FirstOrDefault();

        first.Should().NotBeNull();

        var result = await _gateway.GetCurseDefinitionByKeyAsync(first!.Key);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be(first.Key);
    }

    [Fact]
    public async Task GetCurseDefinitionByKeyAsync_ShouldFail_ForUnknownKey()
    {
        var result = await _gateway.GetCurseDefinitionByKeyAsync("curse-nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.curse_definition_not_found");
    }

    // ── Item Definitions ──────────────────────────────────────────────

    [Fact]
    public async Task GetItemDefinitionByKeyAsync_ShouldReturnSeededItem()
    {
        var result = await _gateway.GetItemDefinitionByKeyAsync("item.consumable.minor-heal");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("item.consumable.minor-heal");
    }

    [Fact]
    public async Task GetItemDefinitionByKeyAsync_ShouldFail_ForUnknownKey()
    {
        var result = await _gateway.GetItemDefinitionByKeyAsync("item-nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.item_definition_not_found");
    }

    // ── Effect Sets ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEffectSetByKeyAsync_ShouldReturnSeededEffectSet()
    {
        var result = await _gateway.GetEffectSetByKeyAsync("effect.item.minor-heal");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("effect.item.minor-heal");
    }

    [Fact]
    public async Task GetEffectSetByKeyAsync_ShouldFail_ForUnknownKey()
    {
        var result = await _gateway.GetEffectSetByKeyAsync("effect-nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.effect_set_not_found");
    }

    // ── Reward Templates ──────────────────────────────────────────────

    [Fact]
    public async Task GetRewardTemplateByKeyAsync_ShouldReturnSeededTemplate()
    {
        var result = await _gateway.GetRewardTemplateByKeyAsync("reward.combat.default");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("reward.combat.default");
    }

    [Fact]
    public async Task GetRewardTemplateByKeyAsync_ShouldFail_ForUnknownKey()
    {
        var result = await _gateway.GetRewardTemplateByKeyAsync("reward-nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("catalog.reward_template_not_found");
    }

    // ── Enemy Definitions ─────────────────────────────────────────────

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldReturnSeededEnemy()
    {
        var result = await _gateway.GetEnemyDefinitionByKeyAsync("enemy.threshold.echo");

        result.Should().NotBeNull();
        result!.Key.Should().Be("enemy.threshold.echo");
    }

    [Fact]
    public async Task GetEnemyDefinitionByKeyAsync_ShouldReturnNull_ForUnknownKey()
    {
        var result = await _gateway.GetEnemyDefinitionByKeyAsync("enemy-nonexistent");

        result.Should().BeNull();
    }

    // ── Skill Definitions ─────────────────────────────────────────────

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldReturnSeededSkill()
    {
        var result = await _gateway.GetSkillDefinitionByKeyAsync("skill.basic.strike");

        result.Should().NotBeNull();
        result!.Key.Should().Be("skill.basic.strike");
    }

    [Fact]
    public async Task GetSkillDefinitionByKeyAsync_ShouldReturnNull_ForUnknownKey()
    {
        var result = await _gateway.GetSkillDefinitionByKeyAsync("skill-nonexistent");

        result.Should().BeNull();
    }

    // ── Room Boss Definitions ─────────────────────────────────────────

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldReturnSeededBoss()
    {
        var result = await _gateway.GetRoomBossProfileAsync("Memory");

        result.Should().NotBeNull();
        result!.RoomType.Should().Be("Memory");
    }

    [Fact]
    public async Task GetRoomBossProfileAsync_ShouldReturnNull_ForUnknownRoomType()
    {
        var result = await _gateway.GetRoomBossProfileAsync("UnknownRoomType");

        result.Should().BeNull();
    }

    // ── NPC Definitions ───────────────────────────────────────────────

    [Fact]
    public async Task ListNpcDefinitionsAsync_ShouldReturnCollection()
    {
        var result = await _gateway.ListNpcDefinitionsAsync();

        result.Should().NotBeNull();
    }

    // ── Anti-regression: no method throws NotAvailableYet ─────────────

    [Fact]
    public void NoMethod_ShouldThrow_NotAvailableYet()
    {
        var gatewayType = typeof(HttpCatalogContentGateway);
        var methods = gatewayType.GetMethods(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance)
            .Where(m => m.DeclaringType == gatewayType);

        foreach (var method in methods)
        {
            if (method.GetParameters().Length == 0)
            {
                continue;
            }

            var parameters = method.GetParameters()
                .Select(p => p.ParameterType == typeof(CancellationToken)
                    ? CancellationToken.None
                    : p.HasDefaultValue
                        ? p.DefaultValue
                        : GetDefault(p.ParameterType))
                .ToArray();

            var invocation = () =>
            {
                try
                {
                    var task = method.Invoke(_gateway, parameters);
                    if (task is Task t)
                    {
                        t.GetAwaiter().GetResult();
                    }
                }
                catch (System.Reflection.TargetInvocationException ex)
                    when (ex.InnerException is CatalogGatewayException cge
                        && cge.Message.Contains("not available"))
                {
                    throw cge;
                }
            };

            invocation.Should().NotThrow<CatalogGatewayException>(
                because: $"{method.Name} should not throw NotAvailableYet");
        }
    }

    private static object? GetDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
