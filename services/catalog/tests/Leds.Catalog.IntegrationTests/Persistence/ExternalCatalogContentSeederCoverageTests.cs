using FluentAssertions;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Leds.Catalog.IntegrationTests.Persistence;

[Collection("CatalogPostgres")]
public sealed class ExternalCatalogContentSeederCoverageTests
{
    private readonly CatalogPostgresFixture _fixture;

    public ExternalCatalogContentSeederCoverageTests(CatalogPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ApplyAsync_ShouldSkipMissingDirectory()
    {
        await using var context = _fixture.CreateContext().Context;
        var missing = Path.Combine(Path.GetTempPath(), $"leds-missing-{Guid.NewGuid():N}");
        var seeder = CreateSeeder(context, missing);

        await seeder.ApplyAsync();

        (await context.NpcDefinitions.CountAsync()).Should().Be(0);
        (await context.RewardCursePools.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ApplyAsync_ShouldSkipEmptyDirectory()
    {
        await using var context = _fixture.CreateContext().Context;
        using var directory = TempDirectory.Create();
        var seeder = CreateSeeder(context, directory.Path);

        await seeder.ApplyAsync();

        (await context.NpcDefinitions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ApplyAsync_ShouldIgnoreMalformedAndNullPacksAndInsertValidContent()
    {
        await using var context = _fixture.CreateContext().Context;
        using var directory = TempDirectory.Create();
        await File.WriteAllTextAsync(System.IO.Path.Combine(directory.Path, "01-malformed.json"), "{ definitely not json");
        await File.WriteAllTextAsync(System.IO.Path.Combine(directory.Path, "02-null.json"), "null");
        await File.WriteAllTextAsync(System.IO.Path.Combine(directory.Path, "03-valid.json"), PackJson("1"));
        var seeder = CreateSeeder(context, directory.Path);

        await seeder.ApplyAsync();

        var npc = await context.NpcDefinitions.SingleAsync(x => x.Key == "npc.coverage");
        npc.Name.Should().Be("Coverage NPC");
        npc.DisplayName.Should().Be("Coverage NPC");
        npc.Description.Should().BeEmpty();
        npc.Status.Should().Be("Active");
        npc.Version.Should().Be("1");
        npc.PersonaJson.Should().BeNull();
        npc.DialogueGraphJson.Should().BeNull();
        npc.TagsJson.Should().Be("[]");
        npc.EncounterKeysJson.Should().Be("[]");

        var pool = await context.RewardCursePools.SingleAsync(x => x.Key == "pool.coverage");
        pool.Description.Should().BeEmpty();
        pool.Status.Should().Be("Active");
        pool.EntriesJson.Should().Be("[]");
    }

    [Fact]
    public async Task ApplyAsync_ShouldNoOpSameVersionThenUpdateChangedVersions()
    {
        await using var context = _fixture.CreateContext().Context;
        using var directory = TempDirectory.Create();
        var file = System.IO.Path.Combine(directory.Path, "content.json");
        await File.WriteAllTextAsync(file, PackJson("1"));
        var seeder = CreateSeeder(context, directory.Path);

        await seeder.ApplyAsync();
        context.ChangeTracker.Clear();
        var firstNpcUpdated = (await context.NpcDefinitions.SingleAsync()).UpdatedAtUtc;
        var firstPoolUpdated = (await context.RewardCursePools.SingleAsync()).UpdatedAtUtc;

        await seeder.ApplyAsync();
        context.ChangeTracker.Clear();
        (await context.NpcDefinitions.SingleAsync()).UpdatedAtUtc.Should().Be(firstNpcUpdated);
        (await context.RewardCursePools.SingleAsync()).UpdatedAtUtc.Should().Be(firstPoolUpdated);

        await File.WriteAllTextAsync(file, PackJson("2", explicitValues: true));
        await seeder.ApplyAsync();
        context.ChangeTracker.Clear();

        var npc = await context.NpcDefinitions.SingleAsync();
        npc.Version.Should().Be("2");
        npc.DisplayName.Should().Be("Visible Coverage NPC");
        npc.Description.Should().Be("Updated");
        npc.Status.Should().Be("Draft");
        npc.TagsJson.Should().Contain("tag-a");
        npc.EncounterKeysJson.Should().Contain("encounter-a");
        npc.PersonaJson.Should().NotBeNull();

        var pool = await context.RewardCursePools.SingleAsync();
        pool.Version.Should().Be("2");
        pool.Description.Should().Be("Updated pool");
        pool.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task ApplyAsync_ShouldRejectNpcWithoutExplicitEmotionalAffinity()
    {
        await using var context = _fixture.CreateContext().Context;
        using var directory = TempDirectory.Create();
        await File.WriteAllTextAsync(
            System.IO.Path.Combine(directory.Path, "invalid.json"),
            PackJson("1").Replace("\"emotionalAffinity\": \"Silence\"", "\"emotionalAffinity\": \" \"", StringComparison.Ordinal));
        var seeder = CreateSeeder(context, directory.Path);

        var act = () => seeder.ApplyAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static ExternalCatalogContentSeeder CreateSeeder(CatalogDbContext context, string path)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CatalogSeed:ContentPath"] = path
            })
            .Build();
        return new ExternalCatalogContentSeeder(
            context,
            configuration,
            NullLogger<ExternalCatalogContentSeeder>.Instance);
    }

    private static string PackJson(string version, bool explicitValues = false)
    {
        var displayName = explicitValues ? "\"Visible Coverage NPC\"" : "null";
        var description = explicitValues ? "\"Updated\"" : "null";
        var status = explicitValues ? "\"Draft\"" : "null";
        var tags = explicitValues ? "[\"tag-a\"]" : "null";
        var roomTypes = explicitValues ? "[\"Npc\"]" : "null";
        var roomStates = explicitValues ? "[\"Freed\"]" : "null";
        var climates = explicitValues ? "[\"Calm\"]" : "null";
        var persona = explicitValues
            ? "{\"tone\":\"Quiet\",\"register\":\"Silence\",\"needs\":[],\"offerings\":[]}"
            : "null";
        var wounds = explicitValues ? "[]" : "null";
        var encounters = explicitValues ? "[\"encounter-a\"]" : "null";
        var poolDescription = explicitValues ? "\"Updated pool\"" : "null";
        var poolStatus = explicitValues ? "\"Draft\"" : "null";
        var entries = "null";

        return $$"""
        {
          "key": "pack.coverage",
          "version": "{{version}}",
          "npcs": [
            {
              "key": "npc.coverage",
              "name": "Coverage NPC",
              "displayName": {{displayName}},
              "description": {{description}},
              "version": "{{version}}",
              "status": {{status}},
              "tags": {{tags}},
              "compatibleRoomTypes": {{roomTypes}},
              "compatiblePalaceRoomStates": {{roomStates}},
              "compatibleRoomClimates": {{climates}},
              "minDepth": null,
              "maxDepth": null,
              "emotionalAffinity": "Silence",
              "isRecurring": false,
              "persona": {{persona}},
              "wounds": {{wounds}},
              "dialogueGraph": null,
              "encounterKeys": {{encounters}}
            }
          ],
          "rewardCursePools": [
            {
              "key": "pool.coverage",
              "name": "Coverage Pool",
              "description": {{poolDescription}},
              "version": "{{version}}",
              "status": {{poolStatus}},
              "entries": {{entries}}
            }
          ]
        }
        """;
    }

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            Path = path;
            Directory.CreateDirectory(path);
        }

        public string Path { get; }

        public static TempDirectory Create() =>
            new(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"leds-catalog-{Guid.NewGuid():N}"));

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
