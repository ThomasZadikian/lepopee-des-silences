using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Leds.Catalog.IntegrationTests;

public sealed class CatalogSeedCompletenessTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .Build();
        await _container.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting(
                    "ConnectionStrings:CatalogDb",
                    _container.GetConnectionString());
            });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await dbContext.Database.MigrateAsync();

        var seedRunner = scope.ServiceProvider.GetRequiredService<CatalogSeedRunner>();
        await seedRunner.ApplyBaseSeedAsync();

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task AllRoomTypes_ShouldHaveRoomBossAndEnemy()
    {
        var roomTypes = new[] { "Threshold", "Memory", "Forest", "Rupture", "Silence", "Antechamber", "Final" };

        foreach (var roomType in roomTypes)
        {
            // 1. GET /api/v2/catalog/room-boss-definitions/room-type/{roomType} → returns a boss (non-null)
            var bossResponse = await _client.GetAsync(
                $"/api/v2/catalog/room-boss-definitions/room-type/{roomType}");

            var bossBody = await bossResponse.Content.ReadAsStringAsync();
            bossResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: $"Room type '{roomType}' should have a room-boss definition. Response: {bossBody}");

            var bossPayload = await bossResponse.Content
                .ReadFromJsonAsync<RoomBossByRoomTypeResponse>();

            bossPayload.Should().NotBeNull();
            bossPayload!.Definition.Should().NotBeNull(
                because: $"Room type '{roomType}' should have a non-null room-boss definition");

            // 2. GET /api/v2/catalog/enemy-definitions/room-type/{roomType} → at least one compatible enemy
            var enemyResponse = await _client.GetAsync(
                $"/api/v2/catalog/enemy-definitions/room-type/{roomType}");

            var enemyBody = await enemyResponse.Content.ReadAsStringAsync();
            enemyResponse.StatusCode.Should().Be(
                HttpStatusCode.OK,
                because: $"Room type '{roomType}' should have compatible enemies. Response: {enemyBody}");

            var enemyPayload = await enemyResponse.Content
                .ReadFromJsonAsync<EnemyListResponse>();

            enemyPayload.Should().NotBeNull();
            enemyPayload!.Definitions.Should().NotBeEmpty(
                because: $"Room type '{roomType}' should have at least one compatible enemy");
        }
    }

    private sealed record RoomBossByRoomTypeResponse(
        RoomBossDto? Definition);

    private sealed record RoomBossDto(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string RoomType,
        int BaseDifficulty,
        IReadOnlyCollection<string> Tags);

    private sealed record EnemyListResponse(
        IReadOnlyCollection<EnemyDto> Definitions);

    private sealed record EnemyDto(
        Guid Id,
        string Key,
        string Name,
        string Description,
        string Version,
        string Status,
        string Archetype,
        int BaseDifficulty,
        int MinRiskLevel,
        int MaxRiskLevel,
        IReadOnlyCollection<string> CompatibleRoomTypes,
        IReadOnlyCollection<string> Tags,
        IReadOnlyCollection<string> SkillKeys);
}
