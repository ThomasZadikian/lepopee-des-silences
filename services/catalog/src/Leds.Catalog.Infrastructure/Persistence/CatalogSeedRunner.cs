using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Domain.Gameplay;
using Leds.Catalog.Domain.Items;
using Leds.Catalog.Domain.Npcs;
using Leds.Catalog.Domain.RewardCursePools;
using Leds.Catalog.Domain.Rewards.Loot;
using Leds.Catalog.Domain.Skills;
using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leds.Catalog.Infrastructure.Persistence;

/// <summary>
/// CONFIDENTIAL — local-only canon content (IP). Git-ignored. Idempotent: upsert by
/// key, updates only when the version differs.
/// </summary>
public sealed partial class CatalogSeedRunner
{
    private static readonly JsonSerializerOptions J = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CatalogDbContext _ctx;
    private readonly ILogger<CatalogSeedRunner> _logger;
    private DateTime _now;

    /// <summary>
    /// Authoring convention only, mirrored from the game-engine service's
    /// CombatTime.TicksPerTurn (catalog and game-engine are separate deployables
    /// with no shared assembly for this). "1 tour" in a skill/status duration means
    /// this many legacy-compatible duration units. The tactical engine converts them
    /// into activations of the status holder.
    /// not at all. Keep this value in sync with the game-engine constant by hand.
    /// </summary>
    private const int TicksPerTurn = 2500;

    public CatalogSeedRunner(CatalogDbContext ctx, ILogger<CatalogSeedRunner> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task ApplyBaseSeedAsync(CancellationToken cancellationToken = default)
    {
        _now = DateTime.UtcNow;

        await SeedMajordomeAsync(cancellationToken);
        await SeedHitomiAsync(cancellationToken);
        await SeedForgeronAsync(cancellationToken);
        await SeedHomonculeAsync(cancellationToken);
        await SeedEnfantAsync(cancellationToken);
        await SeedHimLitAsync(cancellationToken);
        await SeedTovmaAsync(cancellationToken);
        await SeedSathomAsync(cancellationToken);
        await SeedErinaAsync(cancellationToken);
        await SeedPomenianAsync(cancellationToken);
        await SeedOuchianAsync(cancellationToken);
        await SeedIrisAsync(cancellationToken);
        await SeedEthanAsync(cancellationToken);
        await SeedMargotAsync(cancellationToken);
        await SeedAraranAsync(cancellationToken);
        await SeedManeAsync(cancellationToken);
        await SeedThomasAsync(cancellationToken);
        await SeedArchitecteAsync(cancellationToken);
        await SeedEcrivainAsync(cancellationToken);
        await SeedErikaAsync(cancellationToken);
        await SeedMinaAsync(cancellationToken);
        await SeedEliseAsync(cancellationToken);
        await SeedJohnAsync(cancellationToken);
        await SeedEmotionsAsync(cancellationToken);
        await SeedCanonEnemiesAsync(cancellationToken);
        await SeedCanonSkillsAsync(cancellationToken);
        await SeedBestiaireVeilleursDuSeuilAsync(cancellationToken);
        await SeedBestiaireCopistesAsync(cancellationToken);
        await SeedBestiaireSqueletteDeSouvenirsAsync(cancellationToken);
        await SeedBestiaireChimeresDesPlainesAsync(cancellationToken);
        await SeedBestiaireCreationsDuForgeronAsync(cancellationToken);
        await SeedBestiaireBlousesBlanchesAsync(cancellationToken);
        await SeedBestiairePenitentsDeLaMontagneAsync(cancellationToken);
        await SeedBestiaireFauxHabitantsDuJardinAsync(cancellationToken);
        await SeedBestiaireGardiensDeCrystalAsync(cancellationToken);
        await SeedBestiaireEchosDEmotionsAsync(cancellationToken);
        await SeedBestiaireImperatriceDeLaFalaiseAsync(cancellationToken);
        await SeedCanonItemsAsync(cancellationToken);
        await SeedPalaceItemsAsync(cancellationToken);
        await SeedCanonicalWeaponsAsync(cancellationToken);
        await SeedArchetypesAsync(cancellationToken);
        await SeedCanonCursesAsync(cancellationToken);
        await PruneCanonLawPlaceholdersAsync(cancellationToken);
        await SeedLoisMajeuresAsync(cancellationToken);
        await SeedLoisDeCombatAsync(cancellationToken);
        await SeedLoisClimatiquesAsync(cancellationToken);
        await SeedLoisDuSeuilAsync(cancellationToken);
        await SeedLoisEconomieAsync(cancellationToken);
        await SeedEditsClementsAsync(cancellationToken);
        await SeedLoisDeMemoireAsync(cancellationToken);
        await SeedLoisLieesAuxSallesAsync(cancellationToken);
        await SeedCanonRoomsAsync(cancellationToken);
        await SeedPalaisWorldAsync(cancellationToken);
        await SeedRoomThemeAffinitiesAsync(cancellationToken);
        await SeedNpcReputationAffinitiesAsync(cancellationToken);
        await SeedCanonBossesAsync(cancellationToken);
        await SeedCanonRoomTypesAsync(cancellationToken);
        await SeedCanonLootAsync(cancellationToken);
        await SeedPalaceItemLootAsync(cancellationToken);
        await SeedRewardTemplatesAsync(cancellationToken);

        // Sauvegarde inconditionnelle : les SeedCanon*Async ajoutent au change-tracker
        // EF mais ne renvoient pas de compteur ; gater le SaveChanges sur les seuls PNJ
        // faisait silencieusement perdre tout le reste au re-seed.
        await _ctx.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Canon content seeded.");
    }

    // ── Upsert helpers ───────────────────────────────────────────────────────

    private async Task<int> UpsertNpcAsync(
        string key, string name, string description, string version,
        EmotionalRegister affinity, bool recurring,
        NpcPersona persona, IReadOnlyList<NpcWound> wounds, NpcDialogueGraph graph,
        CancellationToken ct,
        IReadOnlyList<string>? boundRoomKeys = null,
        IReadOnlyList<NpcOffering>? offerings = null)
    {
        var existing = await _ctx.NpcDefinitions.FirstOrDefaultAsync(n => n.Key == key, ct);
        if (existing is not null && string.Equals(existing.Version, version, StringComparison.Ordinal))
        {
            return 0;
        }

        var e = existing ?? new NpcDefinitionEntity { Id = Guid.NewGuid(), CreatedAtUtc = _now };
        e.Key = key;
        e.Name = name;
        e.DisplayName = name;
        e.Description = description;
        e.Version = version;
        e.Status = "Active";
        e.MinDepth = null;
        e.MaxDepth = null;
        e.TagsJson = "[]";
        e.CompatibleRoomTypesJson = "[]";
        e.CompatiblePalaceRoomStatesJson = "[]";
        e.CompatibleRoomClimatesJson = "[]";
        e.EmotionalAffinity = EmotionalRegisterCatalog.CodeOf(affinity);
        e.IsRecurring = recurring;
        e.PersonaJson = JsonSerializer.Serialize(persona, J);
        e.WoundsJson = JsonSerializer.Serialize(wounds, J);
        e.DialogueGraphJson = JsonSerializer.Serialize(graph, J);
        e.EncounterKeysJson = "[]";
        e.BoundRoomKeysJson = JsonSerializer.Serialize(boundRoomKeys ?? [], J);
        e.OfferingsJson = JsonSerializer.Serialize(offerings ?? [], J);
        e.UpdatedAtUtc = _now;

        if (existing is null)
        {
            _ctx.NpcDefinitions.Add(e);
        }

        return 1;
    }

    private async Task<int> UpsertNpcReputationAffinityAsync(
        string npcKeyFrom, string npcKeyTo, decimal weight, CancellationToken ct)
    {
        var existing = await _ctx.NpcReputationAffinities
            .FirstOrDefaultAsync(a => a.NpcKeyFrom == npcKeyFrom && a.NpcKeyTo == npcKeyTo, ct);

        if (existing is not null)
        {
            existing.Weight = weight;
            existing.UpdatedAtUtc = _now;
            return 0;
        }

        _ctx.NpcReputationAffinities.Add(new NpcReputationAffinityEntity
        {
            Id = Guid.NewGuid(),
            NpcKeyFrom = npcKeyFrom,
            NpcKeyTo = npcKeyTo,
            Weight = weight,
            CreatedAtUtc = _now,
            UpdatedAtUtc = _now
        });

        return 1;
    }

    // TODO(utilisateur) : deux paires d'affinité confirmées narrativement, aucune convention de
    // valeur pour Weight n'a encore été établie (le champ n'est d'ailleurs consommé par aucune
    // logique de gameplay pour l'instant, juste transporté). Ne pas inventer un nombre ; câbler
    // les paires une fois la valeur confirmée :
    //   - npc.homoncule / npc.enfant : se détestent mutuellement.
    //   - npc.homoncule → npc.forgeron : qui est apprécié par l'Homoncule l'est un peu plus par
    //     le Forgeron (sa création la plus fière) — sens UNIQUEMENT homoncule → forgeron.
    private async Task SeedNpcReputationAffinitiesAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
    }

    private async Task<int> UpsertPoolAsync(
        string key, string name, string description, string version,
        IReadOnlyList<RewardCurseEntry> entries, CancellationToken ct)
    {
        var existing = await _ctx.RewardCursePools.FirstOrDefaultAsync(p => p.Key == key, ct);
        if (existing is not null && string.Equals(existing.Version, version, StringComparison.Ordinal))
        {
            return 0;
        }

        var e = existing ?? new RewardCursePoolEntity { Id = Guid.NewGuid(), CreatedAtUtc = _now };
        e.Key = key;
        e.Name = name;
        e.Description = description;
        e.Version = version;
        e.Status = "Active";
        e.EntriesJson = JsonSerializer.Serialize(entries, J);
        e.UpdatedAtUtc = _now;

        if (existing is null)
        {
            _ctx.RewardCursePools.Add(e);
        }

        return 1;
    }

    private static DialogueConsequence C(
        ConsequenceKind kind, WoundState? when = null, string? frag = null, string? pool = null,
        int rel = 0, string? flag = null, string? wound = null, string? offering = null) =>
        new(kind, when, frag, pool, null, rel, flag, wound, offering, null, null, null);

    // ── Le Majordome (Silence, irréversible) ─────────────────────────────────

    private async Task<int> SeedMajordomeAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Courtois, attentif, d'une politesse glaçante", EmotionalRegister.Silence,
            new[] { "le respect du seuil", "la propreté du tapis" },
            new[] { "thé", "eau", "attention" });

        var wounds = new[]
        {
            new NpcWound("w-tapis", EmotionalRegister.Rupture, NpcWoundReversibility.Irreversible, -2, -4,
                new[] { new NpcTransgression("w-tapis", "tapis-souille", -5) },
                "Le seuil a été souillé. Cela ne se pardonne pas.")
        };

        var seuil = new NpcDialogueNode("seuil", "Le Majordome",
            new[] { "Entrez. Le thé est encore chaud.", "Veillez à vos pas — ce tapis a connu des hôtes moins soigneux." },
            new[]
            {
                new NpcDialogueChoice("salir", "Entrer sans essuyer vos pieds", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "tapis-souille"),
                            C(ConsequenceKind.Narrative, frag: "Vos semelles laissent une trace sombre sur le tapis. Le Majordome ne dit rien.") }, "seuil"),
                new NpcDialogueChoice("boire", "Boire l'eau", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Latent, pool: "pool.majordome.eau-benigne"),
                            C(ConsequenceKind.Narrative, when: WoundState.Rompu, frag: "Le thé a un goût d'amertume que vous ne sauriez nommer."),
                            C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Rompu, pool: "pool.majordome.eau-poison-degats"),
                            C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Rompu, pool: "pool.majordome.eau-poison-malediction") }, null),
                new NpcDialogueChoice("questionner", "L'interroger sur le tapis", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Il sourit. Ses mains, elles, se crispent.") }, "confidence"),
                new NpcDialogueChoice("partir", "S'éloigner", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous reculez. Le Majordome incline la tête, impeccable.") }, null)
            },
            TenseLines: new[] { "Le thé est servi.", "…Vous semblez pressé. J'espère que vous savez où vous mettez les pieds." },
            RupturedLines: new[] { "Vous voilà de retour.", "Le tapis, lui, n'oublie pas. Buvez donc — vous l'avez bien mérité." });

        var confidence = new NpcDialogueNode("confidence", "Le Majordome",
            new[] { "« Le seuil se respecte. Toujours. Ceux qui l'oublient… ne reviennent pas. »" },
            new[]
            {
                new NpcDialogueChoice("comprendre", "Hocher la tête", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.Narrative, frag: "Quelque chose dans son regard s'apaise, à peine.") }, "seuil"),
                new NpcDialogueChoice("don", "Lui demander s'il a quelque chose à offrir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Il incline la tête, comme s'il attendait cette question depuis le début.") }, "don"),
                new NpcDialogueChoice("partir", "S'éloigner", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous le laissez à son seuil.") }, null)
            });

        var don = new NpcDialogueNode("don", "Le Majordome",
            new[] { "« Le thé est toujours prêt pour ceux que je reconnais. » Il vous tend une tasse, sans un geste de trop." },
            new[]
            {
                new NpcDialogueChoice("accepter-tasse-the", "Accepter une tasse de thé",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.majordome.tasse-the") }, null),
                new NpcDialogueChoice("accepter-tasse-majordome", "Accepter la tasse du majordome",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.majordome.tasse-majordome") }, null),
                new NpcDialogueChoice("accepter-sceau", "Accepter le Sceau de l'invité reconnu",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 2000) },
                    new[] { C(ConsequenceKind.Narrative, frag: "Il presse un sceau de cire froide dans votre paume. « Ceux qui l'oublient ne reviennent pas. Vous, si. »"),
                            C(ConsequenceKind.GrantOffering, offering: "offer.majordome.sceau-invite") }, null),
                new NpcDialogueChoice("don-decliner", "Remercier et repartir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Il incline la tête. « À votre service. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.majordome.dialogue", "1.4", "seuil",
            new Dictionary<string, NpcDialogueNode> { ["seuil"] = seuil, ["confidence"] = confidence, ["don"] = don });

        var offerings = new[]
        {
            // Tasse de thé (rare) : toujours resservie une fois le seuil de réputation atteint —
            // répétable (IsMajor: false), pas de plafond de dons.
            new NpcOffering("offer.majordome.tasse-the", NpcOfferingKind.Item, "canon.item.tasse-de-the", 1, false,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) })