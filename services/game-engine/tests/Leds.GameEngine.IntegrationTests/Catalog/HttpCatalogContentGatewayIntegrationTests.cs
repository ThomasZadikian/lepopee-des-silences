extern alias CatalogApi;

using FluentAssertions;
using CatalogApi::Leds.Catalog.Api;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Infrastructure.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
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
        _container = new PostgreSqlBuilder("postgres:16")
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
                builder.UseSetting("CatalogSeed:ApplyOnStartup", "true");
            });

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
        var result = await _gateway.GetItemDefinitionByKeyAsync("canon.item.tome-38");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("canon.item.tome-38");
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
        var result = await _gateway.GetEffectSetByKeyAsync("effect.law.silence-du");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("effect.law.silence-du");
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
        var result = await _gateway.GetRewardTemplateByKeyAsync("reward.item.default");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be("reward.item.default");
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
        var result = await _gateway.GetEnemyDefinitionByKeyAsync("canon.enemy.lamiz");

        result.Should().NotBeNull();
        result!.Key.Should().Be("canon.enemy.lamiz");
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
