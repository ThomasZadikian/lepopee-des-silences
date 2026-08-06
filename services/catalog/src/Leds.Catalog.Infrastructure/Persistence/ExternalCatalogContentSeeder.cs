using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Domain.Npcs;
using Leds.Catalog.Domain.Gameplay;
using Leds.Catalog.Domain.RewardCursePools;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Leds.Catalog.Infrastructure.Persistence;

/// <summary>
/// Seeds confidential catalog content (NPCs, reward/curse pools…) from external JSON
/// packs that live OUTSIDE the repository (git-ignored folder / mounted volume).
/// The mechanism is committed; the content never is. Idempotent: upsert by key,
/// updates only when the pack version differs.
/// </summary>
public sealed class ExternalCatalogContentSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly CatalogDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalCatalogContentSeeder> _logger;

    public ExternalCatalogContentSeeder(
        CatalogDbContext context,
        IConfiguration configuration,
        ILogger<ExternalCatalogContentSeeder> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        var path = _configuration["CatalogSeed:ContentPath"]
            ?? Environment.GetEnvironmentVariable("CATALOG_SEED_PATH")
            ?? "catalog-seed-content";

        if (!Directory.Exists(path))
        {
            _logger.LogInformation("External catalog content directory '{Path}' not found — skipping.", path);
            return;
        }

        var files = Directory
            .GetFiles(path, "*.json", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            _logger.LogInformation("No external content packs in '{Path}'.", path);
            return;
        }

        var now = DateTime.UtcNow;
        var changed = 0;

        foreach (var file in files)
        {
            CatalogContentPack? pack;
            try
            {
                await using var stream = File.OpenRead(file);
                pack = await JsonSerializer.DeserializeAsync<CatalogContentPack>(stream, JsonOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse content pack '{File}'.", file);
                continue;
            }

            if (pack is null)
            {
                continue;
            }

            foreach (var npc in pack.Npcs ?? [])
            {
                changed += await UpsertNpcAsync(npc, now, cancellationToken);
            }

            foreach (var pool in pack.RewardCursePools ?? [])
            {
                changed += await UpsertPoolAsync(pool, now, cancellationToken);
            }
        }

        if (changed > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "External catalog content applied: {Count} upsert(s) from {Files} pack(s).", changed, files.Length);
    }

    private async Task<int> UpsertNpcAsync(NpcPackItem item, DateTime now, CancellationToken ct)
    {
        var existing = await _context.NpcDefinitions.FirstOrDefaultAsync(n => n.Key == item.Key, ct);
        if (existing is null)
        {
            var entity = new NpcDefinitionEntity { Id = Guid.NewGuid(), CreatedAtUtc = now };
            ApplyNpc(entity, item, now);
            _context.NpcDefinitions.Add(entity);
            return 1;
        }

        if (!string.Equals(existing.Version, item.Version, StringComparison.Ordinal))
        {
            ApplyNpc(existing, item, now);
            return 1;
        }

        return 0;
    }

    private static void ApplyNpc(NpcDefinitionEntity e, NpcPackItem item, DateTime now)
    {
        e.Key = item.Key;
        e.Name = item.Name;
        e.DisplayName = string.IsNullOrWhiteSpace(item.DisplayName) ? item.Name : item.DisplayName!;
        e.Description = item.Description ?? string.Empty;
        e.Version = item.Version;
        e.Status = string.IsNullOrWhiteSpace(item.Status) ? "Active" : item.Status!;
        e.MinDepth = item.MinDepth;
        e.MaxDepth = item.MaxDepth;
        e.TagsJson = JsonSerializer.Serialize(item.Tags ?? [], JsonOptions);
        e.CompatibleRoomTypesJson = JsonSerializer.Serialize(item.CompatibleRoomTypes ?? [], JsonOptions);
        e.CompatiblePalaceRoomStatesJson = JsonSerializer.Serialize(item.CompatiblePalaceRoomStates ?? [], JsonOptions);
        e.CompatibleRoomClimatesJson = JsonSerializer.Serialize(item.CompatibleRoomClimates ?? [], JsonOptions);
        e.EmotionalAffinity = string.IsNullOrWhiteSpace(item.EmotionalAffinity)
            ? throw new InvalidOperationException(
                $"NPC pack entry '{item.Key}' must declare EmotionalAffinity explicitly.")
            : EmotionalRegisterCatalog.CodeOf(EmotionalRegisterCatalog.Parse(item.EmotionalAffinity));
        e.IsRecurring = item.IsRecurring;
        e.PersonaJson = item.Persona is null ? null : JsonSerializer.Serialize(item.Persona, JsonOptions);
        e.WoundsJson = JsonSerializer.Serialize(item.Wounds ?? [], JsonOptions);
        e.DialogueGraphJson = item.DialogueGraph is null ? null : JsonSerializer.Serialize(item.DialogueGraph, JsonOptions);
        e.EncounterKeysJson = JsonSerializer.Serialize(item.EncounterKeys ?? [], JsonOptions);
        e.UpdatedAtUtc = now;
    }

    private async Task<int> UpsertPoolAsync(PoolPackItem item, DateTime now, CancellationToken ct)
    {
        var existing = await _context.RewardCursePools.FirstOrDefaultAsync(p => p.Key == item.Key, ct);
        if (existing is null)
        {
            var entity = new RewardCursePoolEntity { Id = Guid.NewGuid(), CreatedAtUtc = now };
            ApplyPool(entity, item, now);
            _context.RewardCursePools.Add(entity);
            return 1;
        }

        if (!string.Equals(existing.Version, item.Version, StringComparison.Ordinal))
        {
            ApplyPool(existing, item, now);
            return 1;
        }

        return 0;
    }

    private static void ApplyPool(RewardCursePoolEntity e, PoolPackItem item, DateTime now)
    {
        e.Key = item.Key;
        e.Name = item.Name;
        e.Description = item.Description ?? string.Empty;
        e.Version = item.Version;
        e.Status = string.IsNullOrWhiteSpace(item.Status) ? "Active" : item.Status!;
        e.EntriesJson = JsonSerializer.Serialize(item.Entries ?? [], JsonOptions);
        e.UpdatedAtUtc = now;
    }

    // ── Pack DTOs (the wire format; nested objects reuse the catalog domain types) ──
    private sealed record CatalogContentPack(
        string? Key,
        string? Version,
        List<NpcPackItem>? Npcs,
        List<PoolPackItem>? RewardCursePools);

    private sealed record NpcPackItem(
        string Key,
        string Name,
        string? DisplayName,
        string? Description,
        string Version,
        string? Status,
        IReadOnlyList<string>? Tags,
        IReadOnlyList<string>? CompatibleRoomTypes,
        IReadOnlyList<string>? CompatiblePalaceRoomStates,
        IReadOnlyList<string>? CompatibleRoomClimates,
        int? MinDepth,
        int? MaxDepth,
        string? EmotionalAffinity,
        bool IsRecurring,
        NpcPersona? Persona,
        List<NpcWound>? Wounds,
        NpcDialogueGraph? DialogueGraph,
        IReadOnlyList<string>? EncounterKeys);

    private sealed record PoolPackItem(
        string Key,
        string Name,
        string? Description,
        string Version,
        string? Status,
        List<RewardCurseEntry>? Entries);
}
