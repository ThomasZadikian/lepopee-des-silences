using System.Text.Json;
using System.Text.Json.Serialization;
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
public sealed class CatalogSeedRunner 
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
    /// AtbConstants.TicksPerTurn (catalog and game-engine are separate deployables
    /// with no shared assembly for this). "1 tour" in a skill/status duration means
    /// this many ticks of the ATB clock — NOT "1 action taken": fill-per-tick varies
    /// per combatant (Speed, investment, relative tempo, momentum), so a fast
    /// combatant can act many times within "N tours" while a slow one acts once or
    /// not at all. Keep this value in sync with the game-engine constant by hand.
    /// </summary>
    private const int TicksPerTurn = 2500;

    public CatalogSeedRunner(CatalogDbContext ctx, ILogger<CatalogSeedRunner> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
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
        await SeedCanonCursesAsync(cancellationToken);
        await SeedCanonLawsAsync(cancellationToken);
        await AttachCanonLawEffectsAsync(cancellationToken);
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
        e.EmotionalAffinity = affinity.ToString();
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
                new NpcDialogueChoice("don-decliner", "Remercier et repartir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Il incline la tête. « À votre service. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.majordome.dialogue", "1.3", "seuil",
            new Dictionary<string, NpcDialogueNode> { ["seuil"] = seuil, ["confidence"] = confidence, ["don"] = don });

        var offerings = new[]
        {
            // Tasse de thé (rare) : toujours resservie une fois le seuil de réputation atteint —
            // répétable (IsMajor: false), pas de plafond de dons.
            new NpcOffering("offer.majordome.tasse-the", NpcOfferingKind.Item, "canon.item.tasse-de-the", 1, false,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.majordome.tasse-majordome", NpcOfferingKind.Item, "canon.item.tasse-du-majordome", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        // TODO(utilisateur) : liaison à une Room précise non fournie à ce stade — ne pas
        // inventer, compléter une fois le contenu reçu.
        var n = await UpsertNpcAsync("npc.majordome", "Le Majordome",
            "Une présence du seuil : il accueille, il sert, il veille. Et il n'oublie rien.", "1.3",
            EmotionalRegister.Silence, true, persona, wounds, graph, ct,
            offerings: offerings);

        n += await UpsertPoolAsync("pool.majordome.eau-benigne", "Eau du Majordome — bienveillante",
            "Ce que l'eau offre quand le seuil est respecté.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", null, 15),
                    new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", null, 9) }, ct);
        n += await UpsertPoolAsync("pool.majordome.eau-poison-degats", "Eau du Majordome — poison",
            "Le poison qui ronge celui qui a souillé le seuil.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Curse, "Damage", null, 14) }, ct);
        n += await UpsertPoolAsync("pool.majordome.eau-poison-malediction", "Eau du Majordome — malédiction",
            "L'ombre que laisse le seuil souillé.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Curse, "GrantCurse", "curse.old-wound") }, ct);
        return n;
    }

    // ── Hitomi (Mémoire, apaisable) ──────────────────────────────────────────

    private async Task<int> SeedHitomiAsync(CancellationToken ct)
    {
        var persona = new NpcPersona("Douce, sage, d'une présence apaisante", EmotionalRegister.Memoire,
            new[] { "la tendresse", "une présence sincère" },
            new[] { "le silence partagé", "une fleur aux pétales tous différents", "sa main" });

        var wounds = new[]
        {
            new NpcWound("w-abandon", EmotionalRegister.Memoire, NpcWoundReversibility.SoothableByScore, -1, -3,
                new[] { new NpcTransgression("w-abandon", "hitomi-presser", -3) },
                "Elle a appris à reconnaître ceux qui partent.")
        };

        var rive = new NpcDialogueNode("rive", "Hitomi",
            new[] { "Assieds-toi. Le lac est si pur qu'on n'ose pas le toucher.", "Reste un instant — le silence, ici, n'est pas vide." },
            new[]
            {
                new NpcDialogueChoice("prendre-main", "Prendre sa main", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Latent, pool: "pool.hitomi.tendresse"),
                            C(ConsequenceKind.AdjustRelationship, when: WoundState.Latent, rel: 2),
                            C(ConsequenceKind.Narrative, when: WoundState.Rompu, frag: "Elle retire doucement sa main. « Pas cette fois. »"),
                            C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Rompu, pool: "pool.hitomi.retrait") }, "partage"),
                new NpcDialogueChoice("silence", "Partager le silence", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.Narrative, frag: "Vous respirez à l'unisson. Le temps se suspend.") }, "partage"),
                new NpcDialogueChoice("presser", "La presser de repartir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "hitomi-presser"),
                            C(ConsequenceKind.Narrative, frag: "Elle baisse les yeux. Quelque chose se referme.") }, "rive"),
                new NpcDialogueChoice("partir", "S'éloigner", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Tu t'éloignes. Elle ne te retient pas.") }, null)
            },
            TenseLines: new[] { "Te revoilà. Tu sembles… pressé.", "Reste, si tu le veux vraiment." },
            RupturedLines: new[] { "Te revoilà. Mais ton regard est déjà ailleurs.", "Va. Je sais reconnaître ceux qui partent." });

        var partage = new NpcDialogueNode("partage", "Hitomi",
            new[] { "Elle te tend une fleur dont chaque pétale est d'une espèce différente." },
            new[]
            {
                new NpcDialogueChoice("accepter", "Accepter la fleur", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.RewardOrCurseRoll, pool: "pool.hitomi.tendresse"),
                            C(ConsequenceKind.AdjustRelationship, rel: 1) }, null),
                new NpcDialogueChoice("rendre", "La remercier et partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "« Garde-la pour toi », souris-tu. Elle hoche la tête.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.hitomi.dialogue", "1.0", "rive",
            new Dictionary<string, NpcDialogueNode> { ["rive"] = rive, ["partage"] = partage });

        // TODO(utilisateur) : offres concrètes (compétence/sort/objet, majeure/générique)
        // non fournies à ce stade — ne pas inventer, compléter une fois le contenu reçu.
        var n = await UpsertNpcAsync("npc.hitomi", "Hitomi",
            "Une présence douce, rencontrée sur un chemin de montagne. Son regard porte un vide ancien.", "1.1",
            EmotionalRegister.Memoire, true, persona, wounds, graph, ct,
            boundRoomKeys: new[] { "room.room08" });
        n += await UpsertPoolAsync("pool.hitomi.tendresse", "Hitomi — tendresse", "La chaleur d'une présence sincère.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", null, 20),
                    new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", null, 14) }, ct);
        n += await UpsertPoolAsync("pool.hitomi.retrait", "Hitomi — retrait", "Le froid d'une main qu'on n'attrape plus.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Curse, "Damage", null, 5) }, ct);
        return n;
    }

    // ── L'Homoncule (Rupture, première création du Forgeron) ─────────────────

    // ── Le Forgeron (Rupture, créateur physique des habitants du Palais) ──────

    private async Task<int> SeedForgeronAsync(CancellationToken ct)
    {
        var persona = new NpcPersona("Rude, franc, brutal — les mots dans le mauvais ordre, jamais conçu pour parler", EmotionalRegister.Rupture,
            new[] { "le silence", "une création qui tient debout" },
            new[] { "qu'on lui parle", "la critique de son travail" });

        var wounds = new[]
        {
            new NpcWound("w-creations-forgeron", EmotionalRegister.Rupture, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[]
                {
                    new NpcTransgression("w-creations-forgeron", "forgeron-critique-creation", -2),
                    new NpcTransgression("w-creations-forgeron", "forgeron-creation-morte", -2)
                },
                "Il ne forge plus. Il fixe le feu, muet, le marteau immobile dans la main.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Le Forgeron",
            new[] { "Toi. Encore un qui vient parler.", "Pas fait pour ça, moi. Parler." },
            new[]
            {
                new NpcDialogueChoice("silence", "Rester silencieux, l'observer travailler", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 2),
                            C(ConsequenceKind.SootheWound, wound: "w-creations-forgeron") }, "forge"),
                new NpcDialogueChoice("complimenter", "Complimenter son travail", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "forge"),
                new NpcDialogueChoice("critiquer", "Critiquer la brutalité de ses créations", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "forgeron-critique-creation"),
                            C(ConsequenceKind.Narrative, frag: "Il se fige. Le marteau retombe, plus fort que nécessaire. « Sors. »") }, null),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Tu recules. Il ne lève même pas les yeux.") }, null)
            },
            TenseLines: new[] { "Toujours là, toi. Pourquoi.", "Parler, parler. Jamais fini." },
            RupturedLines: new[] { "Dehors. DEHORS.", "Fait pour créer, moi. Pas pour ça." });

        var forge = new NpcDialogueNode("forge", "Le Forgeron",
            new[] { "Eux, dehors — formes, je donne. Le reste, pas moi.", "L'Homoncule. Premier. Le meilleur, encore." },
            new[]
            {
                new NpcDialogueChoice("continuer", "Continuer de l'écouter", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don"),
                new NpcDialogueChoice("demander-abandon", "Lui demander pourquoi il a laissé l'Homoncule seul",
                    Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "« Fait. Fini. Après, pas mon travail. » Il ne dit rien de plus."),
                            C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don"),
                new NpcDialogueChoice("annoncer-mort", "Lui annoncer la mort d'une de ses créations",
                    Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "forgeron-creation-morte"),
                            C(ConsequenceKind.Narrative, frag: "Le marteau s'arrête en l'air. Il ne le repose pas. Il ne dit rien.") }, null)
            });

        var don = new NpcDialogueNode("don", "Le Forgeron",
            new[] { "Tiens. Prends. Pas pour toi que je fais. Fait, c'est tout." },
            new[]
            {
                new NpcDialogueChoice("prendre-marteau", "Accepter le Marteau de forge",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.forgeron.marteau") }, null),
                new NpcDialogueChoice("prendre-souffle", "Accepter \"Souffle de la forge\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.forgeron.souffle") }, null),
                new NpcDialogueChoice("don-decliner", "Partir sans rien demander", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Il ne dit rien. Il retourne au feu, comme si tu n'étais déjà plus là.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.forgeron.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["forge"] = forge, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.forgeron.marteau", NpcOfferingKind.Item, "canon.item.marteau-de-forge", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.forgeron.souffle", NpcOfferingKind.Skill, "canon.skill.souffle-de-la-forge", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        var n = await UpsertNpcAsync("npc.forgeron", "Le Forgeron",
            "Créateur des formes physiques des habitants du Palais — il donne un corps, jamais une âme, un pouvoir ou une personnalité (d'autres entités s'en chargent). Solitaire, brutal, à peine capable de discuter. Sa création dont il est le plus fier reste l'Homoncule.", "1.0",
            EmotionalRegister.Rupture, true, persona, wounds, graph, ct,
            boundRoomKeys: new[] { "room.enfer3" },
            offerings: offerings);
        return n;
    }

    private async Task<int> SeedHomonculeAsync(CancellationToken ct)
    {
        var persona = new NpcPersona("Émotif, colérique, pas très malin — mais il ressent tout", EmotionalRegister.Rupture,
            new[] { "être écouté", "être compris" },
            new[] { "une fiole de cristal", "la boîte qu'il garde" });

        var wounds = new[]
        {
            new NpcWound("w-colere-homoncule", EmotionalRegister.Rupture, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-colere-homoncule", "homoncule-contredire", -2) },
                "Il ne t'écoute plus. Il ne fait plus que hurler.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "L'Homoncule",
            new[] { "Tu... tu me regardes. Personne ne me regarde.", "Le Forgeron m'a fait. Puis il m'a laissé ici." },
            new[]
            {
                new NpcDialogueChoice("ecouter", "L'écouter", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.SootheWound, wound: "w-colere-homoncule") }, "ecoute"),
                new NpcDialogueChoice("contredire", "Le contredire", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "homoncule-contredire"),
                            C(ConsequenceKind.Narrative, frag: "Son visage se déforme de rage. « TU NE SAIS RIEN. »") }, null),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Tu t'éloignes. Il continue de fixer le vide que tu laisses derrière toi.") }, null)
            },
            TenseLines: new[] { "ARRÊTE de me fixer comme ça !", "Tu es comme les autres. Tu vas me juger, comme l'enfant me juge." },
            RupturedLines: new[] { "PARS. PARS PARS PARS.", "Tu ne comprends rien. Personne ne comprend rien." });

        var ecoute = new NpcDialogueNode("ecoute", "L'Homoncule",
            new[] { "Le Forgeron voulait un fils. Il a eu moi. Et l'enfant après. L'enfant... l'enfant est tout ce que je ne suis pas.",
                    "Je ne suis pas malin. Je le sais. Mais je RESSENS. Ça compte, non ?" },
            new[]
            {
                new NpcDialogueChoice("continuer-ecouter", "Continuer à l'écouter", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don"),
                new NpcDialogueChoice("rassurer", "Lui dire qu'il compte", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SootheWound, wound: "w-colere-homoncule"),
                            C(ConsequenceKind.AdjustRelationship, rel: 2) }, "don")
            },
            TenseLines: new[] { "Pourquoi tu m'écoutes ? Personne ne m'écoute jamais vraiment." },
            RupturedLines: new[] { "Tu fais semblant. Comme l'enfant faisait semblant." });

        var don = new NpcDialogueNode("don", "L'Homoncule",
            new[] { "Tu... tu veux quelque chose ? Je peux donner. Je sais donner, même si je ne sais rien faire d'autre." },
            new[]
            {
                new NpcDialogueChoice("prendre-fiole", "Accepter la Fiole de cristal", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.homoncule.fiole") }, null),
                new NpcDialogueChoice("demander-boite", "Lui demander la Boîte",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.homoncule.boite") }, null)
            });

        var graph = new NpcDialogueGraph("npc.homoncule.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["ecoute"] = ecoute, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.homoncule.fiole", NpcOfferingKind.Item, "canon.item.fiole-cristal", 1, true,
                Array.Empty<DialogueRequirement>()),
            new NpcOffering("offer.homoncule.boite", NpcOfferingKind.Item, "canon.item.boite-homoncule", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        // Rencontré aux 4 étages des enfers (room.enfer1-4) ; réside au château (room.enfer4)
        // mais BoundRoomKeys ne distingue pas résidence/simple présence — les 4 sont listées.
        var n = await UpsertNpcAsync("npc.homoncule", "L'Homoncule",
            "La première création du Forgeron. Émotif, colérique, pas très malin — il incarne la part sombre de l'enfant, qui le déteste autant qu'il le déteste.", "1.0",
            EmotionalRegister.Rupture, true, persona, wounds, graph, ct,
            boundRoomKeys: new[] { "room.enfer1", "room.enfer2", "room.enfer3", "room.enfer4" },
            offerings: offerings);
        return n;
    }

    // ── L'Enfant (Mémoire, créateur originel du premier Palais) ───────────────

    private async Task<int> SeedEnfantAsync(CancellationToken ct)
    {
        var persona = new NpcPersona("Un enfant, seul depuis toujours — il dessine ce qu'il a créé", EmotionalRegister.Memoire,
            new[] { "être écouté", "de la compagnie", "de l'affection" },
            new[] { "un dessin", "un sort", "une craie" });

        var wounds = new[]
        {
            new NpcWound("w-solitude-enfant", EmotionalRegister.Memoire, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-solitude-enfant", "enfant-abandon", -2) },
                "Il ne dessine plus. Il ne fait plus qu'attendre, seul, dans le silence de sa cellule.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "L'Enfant",
            new[] { "Tu... tu es venu ? Personne ne vient jamais.", "Regarde, j'ai dessiné une nouvelle pièce. Elle n'existe pas encore, mais elle existera." },
            new[]
            {
                new NpcDialogueChoice("rester", "Rester avec lui", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.SootheWound, wound: "w-solitude-enfant") }, "jeu"),
                new NpcDialogueChoice("ignorer", "L'ignorer ostensiblement", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "enfant-abandon"),
                            C(ConsequenceKind.Narrative, frag: "Son regard se vide. Il retourne à son dessin sans un mot.") }, null),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Tu t'éloignes. Il continue de dessiner, seul.") }, null)
            },
            TenseLines: new[] { "Tu vas repartir, comme les autres ?", "Reste encore un peu. S'il te plaît." },
            RupturedLines: new[] { "Tu es parti. Tu es toujours parti.", "Je ne dessine plus pour ceux qui partent." });

        var jeu = new NpcDialogueNode("jeu", "L'Enfant",
            new[] { "« Ça, c'était le premier Palais. Avant que l'Architecte ne vienne tout changer. »" },
            new[]
            {
                new NpcDialogueChoice("jouer", "Jouer avec lui", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don"),
                new NpcDialogueChoice("demander-enferme", "Lui demander pourquoi il est enfermé", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "« L'Homoncule veut me dévorer. Pour redevenir l'être ultime. C'est pour ça qu'on m'a caché ici. »"),
                            C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "L'Enfant",
            new[] { "Tiens. C'est pour toi. Je crée tout le temps — autant que ça serve à quelqu'un." },
            new[]
            {
                new NpcDialogueChoice("accepter-sort", "Accepter \"Construction perpétuelle\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.enfant.sort") }, null),
                new NpcDialogueChoice("accepter-craie", "Accepter la Craie créatrice",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.enfant.craie") }, null),
                new NpcDialogueChoice("don-decliner", "Le remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Le présent retourne contre sa poitrine, sans reproche. « Une prochaine fois, alors. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.enfant.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["jeu"] = jeu, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.enfant.sort", NpcOfferingKind.Skill, "canon.skill.construction-perpetuelle", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) }),
            new NpcOffering("offer.enfant.craie", NpcOfferingKind.Item, "canon.item.craie-creatrice", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) })
        };

        // room.cellule ("Le chateau - La cellule") porte déjà la lore exacte : "le souvenir d'un
        // petit être qui créa la première version du Palais, bien avant que l'Architecte ne
        // vienne imposer ses plans." — c'est sa cellule.
        var n = await UpsertNpcAsync("npc.enfant", "L'Enfant",
            "Le créateur originel du premier Palais — pièces, couloirs, mythes, légendes, tout est né de lui, avant l'Architecte. Enfermé pour le protéger de l'Homoncule, qui voulait le dévorer pour redevenir l'être ultime.", "1.0",
            EmotionalRegister.Memoire, true, persona, wounds, graph, ct,
            boundRoomKeys: new[] { "room.cellule" },
            offerings: offerings);
        return n;
    }

    // Him'Lit — le Seigneur du Palais, boss récurrent (RoomType.Final, tous les
    // BossInterval=10 étages, cf. canon.enemy.himlit). Pas de réputation, pas
    // d'offering : sa fiche ne porte que Persona + Blessure + un graphe de
    // dialogue dont l'entrée est recalculée à chaque rencontre par
    // HimLitDialogueAttitudeResolver (game-engine), selon le nombre de fois où
    // on l'a rencontré et l'état de la dernière room Markov traversée avant lui
    // (sa propre room est toujours Neutral par construction).
    private async Task<int> SeedHimLitAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Le Seigneur du Palais — arrogant, cynique, d'une élégance froide ; jamais chaleureux, toujours distant",
            EmotionalRegister.Silence,
            new[] { "l'obéissance", "le silence qu'on lui doit", "que rien ne lui échappe" },
            new[] { "une révérence", "le silence", "une dernière prière" });

        var wounds = new[]
        {
            new NpcWound("w-controle-himlit", EmotionalRegister.Silence, NpcWoundReversibility.Irreversible, 0, 0,
                Array.Empty<NpcTransgression>(),
                "Il a déjà tout perdu une fois, par la faute de l'Architecte. Il ne laissera plus jamais cela se reproduire.")
        };

        var p1Calme = new NpcDialogueNode("rencontre-p1-calme", "Him'Lit",
            new[]
            {
                "Tiens. Une neuvième chambre, et vous tenez encore debout.",
                "On m'annonce toujours les nouveaux venus avant qu'ils n'arrivent. Vous, on ne m'a rien dit.",
                "Amusant."
            },
            Array.Empty<NpcDialogueChoice>());

        var p1Fracture = new NpcDialogueNode("rencontre-p1-fracture", "Him'Lit",
            new[]
            {
                "La Lune est mauvaise, ce soir. Tant pis pour vous — vous arrivez au mauvais moment.",
                "Ne confondez pas mon silence avec de la patience."
            },
            Array.Empty<NpcDialogueChoice>());

        var p2Calme = new NpcDialogueNode("rencontre-p2-calme", "Him'Lit",
            new[]
            {
                "Encore vous. On dirait que le Palais a une mémoire plus courte que la mienne.",
                "Je commence à retenir votre visage. C'est mauvais signe — pour vous, s'entend."
            },
            Array.Empty<NpcDialogueChoice>());

        var p2Fracture = new NpcDialogueNode("rencontre-p2-fracture", "Him'Lit",
            new[]
            {
                "Vous revenez. Vous revenez toujours. Il y a quelque chose d'obscène dans votre insistance.",
                "Taisez-vous. Le Palais parle assez fort sans vous."
            },
            Array.Empty<NpcDialogueChoice>());

        var p3Calme = new NpcDialogueNode("rencontre-p3-calme", "Him'Lit",
            new[]
            {
                "Vous êtes devenu une habitude. Je n'aime pas les habitudes que je n'ai pas choisies.",
                "Chaque étage que vous forcez m'appartient un peu moins. Je vais corriger cela."
            },
            Array.Empty<NpcDialogueChoice>());

        var p3Fracture = new NpcDialogueNode("rencontre-p3-fracture", "Him'Lit",
            new[]
            {
                "Assez. Vous n'êtes rien — une fuite dans les fondations, et je répare les fuites.",
                "Vous croyez me connaître ? Personne ne me connaît. Pas même l'Architecte, et regardez ce qu'il m'a coûté."
            },
            Array.Empty<NpcDialogueChoice>());

        var graph = new NpcDialogueGraph("npc.himlit.dialogue", "1.0", "rencontre-p1-calme",
            new Dictionary<string, NpcDialogueNode>
            {
                ["rencontre-p1-calme"] = p1Calme,
                ["rencontre-p1-fracture"] = p1Fracture,
                ["rencontre-p2-calme"] = p2Calme,
                ["rencontre-p2-fracture"] = p2Fracture,
                ["rencontre-p3-calme"] = p3Calme,
                ["rencontre-p3-fracture"] = p3Fracture
            });

        return await UpsertNpcAsync("npc.himlit", "Him'Lit",
            "Le Seigneur du Palais. Il ne dirige que le Palais, mais d'une main de fer — arrogant, cynique, d'une élégance distante. Sa blessure : le contrôle absolu, depuis la trahison de l'Architecte.",
            "1.0", EmotionalRegister.Silence, true, persona, wounds, graph, ct);
    }

    // Tovma — une des projections de l'Architecte, sa part calme et réfléchie :
    // un érudit occultiste assoiffé de tout savoir. Comme toutes les projections
    // de l'Architecte, il relève du registre Mélancolie.
    private async Task<int> SeedTovmaAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Une projection calme de l'Architecte — un érudit occultiste, assoiffé de tout savoir",
            EmotionalRegister.Melancolie,
            new[] { "comprendre", "des symboles inconnus", "un savoir qu'on lui refuse" },
            new[] { "un secret", "un symbole occulte", "une vérité" });

        var wounds = new[]
        {
            new NpcWound("w-connaissance-tovma", EmotionalRegister.Melancolie, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-connaissance-tovma", "tovma-moquerie", -2) },
                "Il ne dort plus, ne mange plus. Il ne fait que chercher — un symbole de plus, un fragment de plus, comme si le prochain allait enfin tout expliquer.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Tovma",
            new[]
            {
                "Vous marchez ici comme si le Palais n'avait rien à vous apprendre. C'est une erreur — tout, ici, veut être lu.",
                "Asseyez-vous. Ou ne le faites pas. Le temps ne signifie plus grand-chose, pour moi."
            },
            new[]
            {
                new NpcDialogueChoice("rester", "Rester l'écouter", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.SootheWound, wound: "w-connaissance-tovma") }, "savoir"),
                new NpcDialogueChoice("moquerie", "Se moquer de son obsession", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "tovma-moquerie"),
                            C(ConsequenceKind.Narrative, frag: "Il ne relève même pas. Il retourne à ses symboles, comme si vous n'aviez rien dit.") }, null),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous vous éloignez. Il ne vous regarde pas partir — trop occupé à chercher.") }, null)
            });

        var savoir = new NpcDialogueNode("savoir", "Tovma",
            new[] { "Chaque symbole que je perce m'en révèle dix que je ne comprends pas encore. C'est insupportable. C'est merveilleux." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Tovma",
            new[] { "Tenez. Ce que j'ai trouvé ne me sert à rien si personne ne l'emporte." },
            new[]
            {
                new NpcDialogueChoice("accepter-main", "Accepter la Main de Khasma",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.tovma.main") }, null),
                new NpcDialogueChoice("accepter-lunettes", "Accepter les Lunettes d'érudit",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.tovma.lunettes") }, null),
                new NpcDialogueChoice("don-decliner", "Remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "L'objet retourne dans la besace, sans un mot de plus. « Il attendra. Il a l'habitude. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.tovma.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["savoir"] = savoir, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.tovma.main", NpcOfferingKind.Item, "canon.item.main-de-khasma", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) }),
            new NpcOffering("offer.tovma.lunettes", NpcOfferingKind.Item, "canon.item.lunettes-erudit", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) })
        };

        return await UpsertNpcAsync("npc.tovma", "Tovma",
            "Une des projections de l'Architecte — sa part calme et réfléchie. Un érudit occultiste qui croit aux forces obscures et aux symboles occultes, assoiffé de tout savoir.",
            "1.0", EmotionalRegister.Melancolie, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    private async Task<int> SeedSathomAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Une projection combative de l'Architecte — volontaire, toujours prêt à avancer et à se battre pour qui le lui demande",
            EmotionalRegister.Melancolie,
            new[] { "avancer", "se battre pour les autres", "ne jamais reculer" },
            new[] { "une demande d'aide", "un adversaire", "quelqu'un en danger" });

        var wounds = new[]
        {
            new NpcWound("w-sauver-sathom", EmotionalRegister.Melancolie, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-sauver-sathom", "sathom-reproche", -2) },
                "Il se souvient d'un visage qu'il n'a pas pu sauver. Depuis, chaque combat est une façon de rattraper ça — comme si gagner cette fois-ci effaçait la fois où il a perdu.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Sathom",
            new[]
            {
                "Vous avez besoin d'aide ? Demandez. Je ne recule devant rien — pas devant un adversaire, pas devant vous.",
                "Rester immobile, ça, je ne sais pas faire."
            },
            new[]
            {
                new NpcDialogueChoice("demander-aide", "Lui demander de l'aide", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "aide"),
                new NpcDialogueChoice("reproche", "Lui reprocher de ne pas avoir su sauver quelqu'un", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "sathom-reproche"),
                            C(ConsequenceKind.Narrative, frag: "Il se raidit, mais ne répond pas. Quelque chose dans son regard se ferme.") }, null),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Il reste là, prêt, comme toujours, au cas où quelqu'un aurait besoin de lui.") }, null)
            });

        var aide = new NpcDialogueNode("aide", "Sathom",
            new[] { "Je ne suis fait que pour ça : avancer, me battre, aider. Le reste, je ne sais pas y faire." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Sathom",
            new[] { "Tenez. Si vous avancez, autant avancer armé." },
            new[]
            {
                new NpcDialogueChoice("accepter-potion", "Accepter une potion de vie",
                    new[]
                    {
                        new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250),
                        new DialogueRequirement(DialogueRequirementKind.PlayerHasContainerItem)
                    },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.sathom.potion") }, null),
                new NpcDialogueChoice("accepter-bague", "Accepter la Bague du courage",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 500) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.sathom.bague") }, null),
                new NpcDialogueChoice("don-decliner", "Remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "L'offre se referme aussi vite qu'elle s'était ouverte. Rien de plus n'est dit.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.sathom.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["aide"] = aide, ["don"] = don });

        var offerings = new[]
        {
            // Toujours donnée quand la réputation est "rare" (>=250) ET que le joueur possède
            // un récipient — répétable (IsMajor: false), pas de plafond de dons.
            new NpcOffering("offer.sathom.potion", NpcOfferingKind.Item, "canon.item.potion-de-vie", 1, false,
                new[]
                {
                    new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250),
                    new DialogueRequirement(DialogueRequirementKind.PlayerHasContainerItem)
                }),
            new NpcOffering("offer.sathom.bague", NpcOfferingKind.Item, "canon.item.bague-du-courage", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 500) })
        };

        return await UpsertNpcAsync("npc.sathom", "Sathom",
            "Une des projections de l'Architecte — sa part combative et volontaire. Toujours prêt à avancer, il aide sans hésiter ceux qui le lui demandent.",
            "1.0", EmotionalRegister.Melancolie, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    private async Task<int> SeedErinaAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Une adolescente arrogante, pleine de questions — très aimante mais incapable de le montrer, elle ne rêve que d'être libre d'aller où elle veut",
            EmotionalRegister.Rupture,
            new[] { "la liberté", "poser des questions", "aller où elle veut" },
            new[] { "l'autorité", "qu'on lui donne des ordres", "l'enfermement" });

        var wounds = new[]
        {
            new NpcWound("w-enfermement-erina", EmotionalRegister.Rupture, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-enfermement-erina", "erina-autorite", -2) },
                "Elle a été enfermée, longtemps, pour son bien disait-on. Depuis, la moindre voix qui prétend savoir mieux qu'elle ce qui est bon pour elle la fait se refermer comme un poing.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Erina",
            new[]
            {
                "Encore quelqu'un qui va me dire quoi faire, c'est ça ? Vous avez cette tête-là.",
                "Posez vos questions si vous voulez. Moi, j'en ai des centaines."
            },
            new[]
            {
                new NpcDialogueChoice("questionner", "Lui poser une question, sans rien exiger", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "liberte"),
                new NpcDialogueChoice("ordonner", "Lui donner un ordre", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "erina-autorite"),
                            C(ConsequenceKind.Narrative, frag: "Elle se ferme d'un coup, comme un mur. Elle ne vous adressera plus la parole aussi facilement.") }, null),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Elle vous regarde à peine — trop occupée à chercher la sortie qu'elle n'a pas encore trouvée.") }, null)
            });

        var liberte = new NpcDialogueNode("liberte", "Erina",
            new[] { "Vous voulez savoir pourquoi je pose tant de questions ? Parce que personne ne m'a jamais laissée demander. Alors maintenant, je ne m'arrête plus." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Erina",
            new[] { "Tenez. Si vous partez, autant partir vite." },
            new[]
            {
                new NpcDialogueChoice("accepter-reve", "Accepter le Rêve d'Erina",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.erina.reve") }, null),
                new NpcDialogueChoice("accepter-liberte", "Accepter La liberté retrouvée",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.erina.liberte") }, null),
                new NpcDialogueChoice("don-decliner", "Remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Un haussement d'épaules, presque soulagé. « Comme vous voulez, alors. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.erina.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["liberte"] = liberte, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.erina.reve", NpcOfferingKind.Item, "canon.item.reve-erina", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.erina.liberte", NpcOfferingKind.Skill, "canon.skill.liberte-retrouvee", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync("npc.erina", "Erina",
            "Une adolescente arrogante, pleine de questions. Très aimante sans jamais le montrer, elle ne veut qu'une chose : être libre d'aller où elle veut.",
            "1.0", EmotionalRegister.Rupture, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    private async Task<int> SeedPomenianAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Un professeur archéologue, arrogant et académique, spécialiste des anciennes religions — persuadé que rien de ce qui ne vient pas d'un livre, d'une école ou d'un enseignement officiel ne mérite d'être appelé savoir",
            EmotionalRegister.Deni,
            new[] { "les livres", "l'enseignement officiel", "avoir raison" },
            new[] { "qu'on remette en cause son savoir", "l'idée que le Palais existe réellement", "être pris en défaut" });

        var wounds = new[]
        {
            new NpcWound("w-ego-pomenian", EmotionalRegister.Deni, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-ego-pomenian", "pomenian-savoir-remis-en-cause", -2) },
                "Son savoir est toute son armure. La remettre en cause ne le blesse pas — ça le rend méprisant, comme si mépriser suffisait à n'avoir jamais tort.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Pomenian",
            new[]
            {
                "Le Palais ? Une fable de plus, comme il en pullule dans les manuscrits mal traduits. Rien de tout cela n'existe — pas au sens où l'entendrait un esprit sérieux.",
                "J'ai passé une vie à étudier les religions anciennes. Croyez-moi : ce que je ne trouve dans aucun livre n'est pas de la connaissance. Ce n'est qu'une superstition de plus."
            },
            new[]
            {
                new NpcDialogueChoice("le-contredire", "Lui dire que le Palais est bien réel, qu'il s'y trouve en ce moment même", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "pomenian-savoir-remis-en-cause"),
                            C(ConsequenceKind.Narrative, frag: "Son visage se ferme. L'arrogance, d'un coup, devient mépris — pour vous, et pour l'idée même que vous puissiez savoir quelque chose qu'il ignore.") }, null),
                new NpcDialogueChoice("ecouter-savoir", "L'écouter développer sa théorie, sans le contredire", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "connaissance"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Il ne lève pas les yeux de ses notes — trop occupé à avoir raison, seul.") }, null)
            });

        var connaissance = new NpcDialogueNode("connaissance", "Pomenian",
            new[] { "Peu importe où nous sommes : la méthode prime sur le lieu. Tant qu'on raisonne comme il faut, le reste s'explique — toujours." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Pomenian",
            new[] { "Tenez. Un objet de mon cabinet — et un peu de ce que les livres m'ont enseigné, si vous savez vous en servir." },
            new[]
            {
                new NpcDialogueChoice("accepter-monocle", "Accepter le monocle de Pomenian",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.pomenian.monocle") }, null),
                new NpcDialogueChoice("accepter-connaissance", "Accepter Connaissance académique",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.pomenian.connaissance") }, null),
                new NpcDialogueChoice("don-decliner", "Remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "L'objet retrouve sa place dans le cabinet, avec le même soin méticuleux. « Dommage. C'était un bon spécimen. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.pomenian.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["connaissance"] = connaissance, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.pomenian.monocle", NpcOfferingKind.Item, "canon.item.monocle-pomenian", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.pomenian.connaissance", NpcOfferingKind.Skill, "canon.skill.connaissance-academique", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync("npc.pomenian", "Pomenian",
            "Un professeur archéologue, arrogant et académique, spécialiste des anciennes religions. Persuadé que seul un enseignement officiel produit un vrai savoir, il est dans le déni total de la réalité du Palais.",
            "1.0", EmotionalRegister.Deni, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    private async Task<int> SeedOuchianAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Un géologue spécialisé dans les vieux temples — calme, méthodique, tout entier concentré sur sa recherche, presque habité par elle",
            EmotionalRegister.Memoire,
            new[] { "les vieux temples", "les vestiges", "la preuve que le passé a existé" },
            new[] { "qu'on lui dise que les temples anciens ne sont qu'une invention du Palais", "que rien n'ait existé avant le Palais", "que sa vie de recherche n'ait servi à rien" });

        var wounds = new[]
        {
            new NpcWound("w-passe-ouchian", EmotionalRegister.Memoire, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-passe-ouchian", "ouchian-passe-nie", -2) },
                "Toute sa vie tient dans une conviction : quelque chose a existé avant le Palais. La lui retirer, c'est lui retirer jusqu'à la raison de creuser.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Ouchian",
            new[]
            {
                "Regardez cette strate. Cette pierre a vu des siècles que le Palais ne connaîtra jamais.",
                "Les vieux temples ne mentent pas. Contrairement à ce qu'on voudrait vous faire croire."
            },
            new[]
            {
                new NpcDialogueChoice("nier-passe", "Lui dire que les anciens temples ne sont qu'une invention du Palais", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "ouchian-passe-nie"),
                            C(ConsequenceKind.Narrative, frag: "Il ne hausse pas la voix. Il se contente de se taire, et de retourner à sa pierre, seul.") }, null),
                new NpcDialogueChoice("sinteresser", "Lui demander ce qu'il a trouvé", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "recherche"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Il ne lève pas les yeux — trop concentré sur sa strate pour remarquer votre départ.") }, null)
            });

        var recherche = new NpcDialogueNode("recherche", "Ouchian",
            new[] { "Ici. Une inscription à moitié effacée. Si j'ai raison, elle prouve qu'un peuple a vécu ici bien avant que quiconque n'ait tracé le premier mur du Palais." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Ouchian",
            new[] { "Tenez. Une pierre que j'ai extraite moi-même. Elle a traversé plus de temps que nous deux réunis." },
            new[]
            {
                new NpcDialogueChoice("accepter-pierre", "Accepter la Pierre antique",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.ouchian.pierre") }, null),
                new NpcDialogueChoice("don-decliner", "Remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "La pierre retourne dans une poche, sans un geste de plus. « Elle a attendu plus longtemps que ça. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.ouchian.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["recherche"] = recherche, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.ouchian.pierre", NpcOfferingKind.Item, "canon.item.pierre-antique", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) })
        };

        return await UpsertNpcAsync("npc.ouchian", "Ouchian",
            "Un géologue spécialisé dans les vieux temples. Calme et méthodique, il consacre sa vie à prouver qu'un passé a existé avant le Palais — et refuse d'entendre que ce passé pourrait n'être qu'une de ses inventions.",
            "1.0", EmotionalRegister.Memoire, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    // TODO(utilisateur) : Iris "juge à travers les yeux d'Ethan" — toute personne
    // qu'Ethan n'apprécie pas, elle a énormément de mal à l'apprécier. Non modélisé
    // mécaniquement ici (Ethan n'existe pas encore comme PNJ) ; à envisager plus tard
    // comme un DialogueRequirement lisant la relation du joueur avec npc.ethan.
    private async Task<int> SeedIrisAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Une fillette de 16 ans, imaginée par le Palais à partir des restes de conscience d'une ancienne aventurière perdue — calme et maternelle malgré son âge, d'une tendresse presque déplacée pour quelqu'un de si jeune",
            EmotionalRegister.Effroi,
            new[] { "Ethan", "rester près de lui", "veiller sur ceux qu'elle aime" },
            new[] { "être éloignée d'Ethan", "apprendre qu'Ethan est mort", "le perdre pour de bon" });

        var wounds = new[]
        {
            new NpcWound("w-ethan-iris", EmotionalRegister.Effroi, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[]
                {
                    new NpcTransgression("w-ethan-iris", "iris-eloignee-ethan", -2),
                    new NpcTransgression("w-ethan-iris", "iris-ethan-mort", -2)
                },
                "Elle n'existe presque que par lui. L'en éloigner, ou lui apprendre qu'il est mort, c'est menacer la seule chose qui la retient encore d'elle-même.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Iris",
            new[]
            {
                "Chut. Ethan dort encore. Il faut du calme, autour de lui — toujours.",
                "Vous êtes gentil ? Ethan dit qu'on ne peut jamais vraiment savoir, avec les gens d'ici."
            },
            new[]
            {
                new NpcDialogueChoice("menacer-eloigner", "Suggérer qu'on pourrait bien l'éloigner d'Ethan", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "iris-eloignee-ethan"),
                            C(ConsequenceKind.Narrative, frag: "Elle se recroqueville d'un coup, comme prête à fuir. « Non. Non, vous ne pouvez pas. »") }, null),
                new NpcDialogueChoice("annoncer-mort", "Lui dire qu'Ethan pourrait bien mourir, un jour", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "iris-ethan-mort"),
                            C(ConsequenceKind.Narrative, frag: "Son calme se brise net. « Non. Il ne peut pas. Pas lui. »") }, null),
                new NpcDialogueChoice("parler-ethan", "Lui demander qui est Ethan", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "ethan"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Elle ne vous suit pas des yeux — seulement Ethan, endormi près d'elle.") }, null)
            });

        var ethan = new NpcDialogueNode("ethan", "Iris",
            new[] { "Ethan, c'est... tout. Il m'a trouvée quand je n'étais qu'un écho. Depuis, je reste près de lui. Toujours." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Iris",
            new[] { "Tenez. Vous avez été gentil — enfin, gentil comme Ethan l'entendrait. Ça compte, pour moi." },
            new[]
            {
                new NpcDialogueChoice("accepter-doudou", "Accepter le Doudou de Ethan",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.iris.doudou") }, null),
                new NpcDialogueChoice("accepter-regard", "Accepter \"Regard infantile\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.iris.regard") }, null),
                new NpcDialogueChoice("don-decliner", "Le remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Elle range l'objet contre elle. « Pas encore, alors. » Elle retourne près d'Ethan sans un mot de plus.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.iris.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["ethan"] = ethan, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.iris.doudou", NpcOfferingKind.Item, "canon.item.doudou-ethan", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.iris.regard", NpcOfferingKind.Skill, "canon.skill.regard-infantile", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync("npc.iris", "Iris",
            "Une fillette de 16 ans, imaginée par le Palais à partir des restes de conscience d'une ancienne aventurière perdue. Calme et maternelle malgré son âge, elle ne vit que pour rester près d'Ethan.",
            "1.0", EmotionalRegister.Effroi, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    private async Task<int> SeedEthanAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Un enfant de 8 ans, la prochaine proie du Palais qui le dévore doucement — silencieux, presque muet, totalement traumatisé par la vision de sa propre dévoration",
            EmotionalRegister.Silence,
            new[] { "le silence", "ne pas être vu", "Iris" },
            new[] { "qu'on le regarde en face", "qu'on lui parle de ce qu'il a vu", "le Palais" });

        var wounds = new[]
        {
            new NpcWound("w-devoration-ethan", EmotionalRegister.Silence, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-devoration-ethan", "ethan-devoration-evoquee", -2) },
                "Il a vu le Palais commencer à le dévorer. Ce qu'il a vu ne se dit pas — ça se tait, ou ça se brise.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Ethan",
            new[]
            {
                "Il ne dit rien. Ses yeux ne quittent jamais la même direction — comme si quelque chose, quelque part, continuait de le regarder.",
                "« ...Le Palais... il... » Sa voix se perd avant la fin de la phrase."
            },
            new[]
            {
                new NpcDialogueChoice("evoquer-devoration", "Lui demander ce qu'il a vu", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "ethan-devoration-evoquee"),
                            C(ConsequenceKind.Narrative, frag: "Il se fige complètement. Plus un mot, plus un geste — comme s'il n'était déjà plus tout à fait là.") }, null),
                new NpcDialogueChoice("rester-silence", "Rester en silence avec lui, sans rien demander", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "silence"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Il ne bouge pas. Il ne bouge presque jamais.") }, null)
            });

        var silence = new NpcDialogueNode("silence", "Ethan",
            new[] { "Il ne parle toujours pas. Mais quelque chose, dans son regard, se détend un peu — comme si le silence, pour une fois, ne lui faisait pas peur." },
            new[]
            {
                new NpcDialogueChoice("rester-encore", "Rester encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Ethan",
            new[] { "Il tend la main. Pas un mot — juste ça." },
            new[]
            {
                new NpcDialogueChoice("accepter-frayeur", "Accepter \"Frayeur organique\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.ethan.frayeur") }, null),
                new NpcDialogueChoice("accepter-bague", "Accepter la Bague de Iris",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.ethan.bague") }, null),
                new NpcDialogueChoice("don-decliner", "Le remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Il retire sa main, referme les doigts sur ce qu'il tenait, et se replonge dans son silence.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.ethan.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["silence"] = silence, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.ethan.frayeur", NpcOfferingKind.Skill, "canon.skill.frayeur-organique", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.ethan.bague", NpcOfferingKind.Item, "canon.item.bague-iris", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync("npc.ethan", "Ethan",
            "Un enfant de 8 ans, la prochaine proie du Palais qui le dévore doucement. Silencieux, presque muet, il reste totalement traumatisé par la vision de sa propre dévoration.",
            "1.0", EmotionalRegister.Silence, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    private async Task<int> SeedMargotAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Une surveillante de l'orphelinat, d'une violence psychologique implacable sous des dehors maternels — sa mission : border les enfants dans une fausse confiance, pour qu'ils cessent de se méfier du lieu qui les dévore",
            EmotionalRegister.Deni,
            new[] { "le confort apparent des enfants", "Ethan", "que l'orphelinat reste un lieu de confiance" },
            new[] { "qu'on nomme ce qu'elle fait vraiment", "qu'un enfant cesse de lui faire confiance", "qu'on protège Ethan de son influence" });

        var wounds = new[]
        {
            new NpcWound("w-cruaute-margot", EmotionalRegister.Deni, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-cruaute-margot", "margot-cruaute-nommee", -2) },
                "Elle appelle ça border, veiller, aimer. Le nommer autrement — cruauté, manipulation — c'est lui retirer le seul mot qui lui permet de continuer.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Margot",
            new[]
            {
                "Les enfants sont en sécurité ici. Je veille sur chacun d'eux, vous savez. Surtout sur Ethan.",
                "Le malheur, dehors, c'est une chose terrible. Ici, au moins, on s'occupe d'eux."
            },
            new[]
            {
                new NpcDialogueChoice("nommer-cruaute", "Lui dire que ce qu'elle fait aux enfants est de la cruauté, pas de l'affection", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "margot-cruaute-nommee"),
                            C(ConsequenceKind.Narrative, frag: "Son sourire ne bouge pas. Mais quelque chose, dans son regard, se durcit d'un coup.") }, null),
                new NpcDialogueChoice("jouer-le-jeu", "La laisser parler de son \"dévouement\" sans la contredire", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "devouement"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Elle vous suit du regard, son sourire déjà revenu vers le prochain enfant.") }, null)
            });

        var devouement = new NpcDialogueNode("devouement", "Margot",
            new[] { "Elle parle d'Ethan longuement — de sa fragilité, de son silence, de tout ce qu'elle fait pour « l'aider à s'ouvrir ». Elle ne remarque pas, ou ne veut pas remarquer, à quel point ses mots sonnent faux." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Margot",
            new[] { "Elle sourit. « Vous méritez une récompense, pour votre... compréhension. »" },
            new[]
            {
                new NpcDialogueChoice("accepter-bonus-rare", "Accepter cinq points de compétence",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.margot.bonus-rare") }, null),
                new NpcDialogueChoice("accepter-bonus-legendaire", "Accepter dix points de compétence",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.margot.bonus-legendaire") }, null),
                new NpcDialogueChoice("don-decliner", "Le remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Elle range sa récompense avec un sourire qui ne change pas. « Une prochaine fois, peut-être. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.margot.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["devouement"] = devouement, ["don"] = don });

        var offerings = new[]
        {
            // IsMajor: true — chaque bonus n'est accordé qu'une seule fois, comme demandé.
            new NpcOffering("offer.margot.bonus-rare", NpcOfferingKind.StatPoint, null, 5, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.margot.bonus-legendaire", NpcOfferingKind.StatPoint, null, 10, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync("npc.margot", "Margot",
            "Une surveillante de l'orphelinat, d'une violence psychologique implacable sous des dehors maternels. Sa mission : border les enfants dans une fausse confiance. Elle nourrit une affection particulière pour Ethan — et le malheur qui l'accable la ravit, puisqu'il nourrit l'orphelinat, et donc le Palais.",
            "1.0", EmotionalRegister.Deni, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    // TODO(utilisateur) : Araran, Tovma et Mané s'apprécient/se méfient en miroir —
    // qui n'est pas aimé par l'un des trois a du mal à être aimé des deux autres.
    // Les trois fiches existent désormais, mais le couplage croisé (DialogueRequirement
    // basé sur la réputation des deux autres) n'est toujours pas modélisé mécaniquement ;
    // à construire plus tard si le besoin se confirme.
    private async Task<int> SeedAraranAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Une des meilleures amies de Tovma — autoritaire, très directe, d'une pertinence redoutable. Elle sait ce qu'est le Palais et devine facilement ses desseins",
            EmotionalRegister.Effroi,
            new[] { "Mané", "Tovma", "comprendre les desseins du Palais" },
            new[] { "que Mané souffre", "qu'on manque de respect à Tovma ou Mané" });

        var wounds = new[]
        {
            new NpcWound("w-mane-araran", EmotionalRegister.Effroi, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-mane-araran", "araran-mane-menace", -2) },
                "Sa clairvoyance s'arrête là où commence Mané. Le mettre en danger, même en mots, est la seule chose qui la fasse vraiment trembler.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Araran",
            new[]
            {
                "Le Palais ? Je sais ce que c'est. Je devine assez bien ce qu'il veut, même quand il ne le dit pas.",
                "Tovma me fait confiance. Ça devrait vous rassurer, ou vous inquiéter — à vous de voir."
            },
            new[]
            {
                new NpcDialogueChoice("menacer-mane", "Suggérer que Mané pourrait être en danger ici", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "araran-mane-menace"),
                            C(ConsequenceKind.Narrative, frag: "Elle se redresse d'un coup. Son ton, déjà direct, devient tranchant.") }, null),
                new NpcDialogueChoice("parler-palais", "Lui demander ce qu'elle sait du Palais", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "desseins"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Elle vous regarde partir, déjà en train d'en tirer une conclusion.") }, null)
            });

        var desseins = new NpcDialogueNode("desseins", "Araran",
            new[] { "Elle expose, sans détour, ce qu'elle a compris du Palais — méthodique, sans une once de doute dans la voix." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Araran",
            new[] { "Elle vous tend quelque chose. « Pour votre clairvoyance à vous — vous en aurez besoin. »" },
            new[]
            {
                new NpcDialogueChoice("accepter-clairvoyance", "Accepter \"Clairvoyance\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.araran.clairvoyance") }, null),
                new NpcDialogueChoice("accepter-faveur", "Accepter sa faveur auprès de Tovma et Mané",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.araran.faveur-tovma"),
                            C(ConsequenceKind.GrantOffering, offering: "offer.araran.faveur-mane") }, null),
                new NpcDialogueChoice("don-decliner", "Le remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Elle range l'objet, sans surprise ni déception. « Le moment n'est pas encore venu. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.araran.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["desseins"] = desseins, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.araran.clairvoyance", NpcOfferingKind.Skill, "canon.skill.clairvoyance", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            // IsMajor: true — chaque faveur n'est accordée qu'une seule fois.
            new NpcOffering("offer.araran.faveur-tovma", NpcOfferingKind.ReputationBoost, "npc.tovma", 250, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) }),
            new NpcOffering("offer.araran.faveur-mane", NpcOfferingKind.ReputationBoost, "npc.mane", 250, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync("npc.araran", "Araran",
            "Une des meilleures amies de Tovma. Autoritaire, très directe, d'une pertinence redoutable, elle sait ce qu'est le Palais et devine facilement ses desseins.",
            "1.0", EmotionalRegister.Effroi, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    // NB(auteur) : le déclencheur/la blessure de Mané n'a pas été précisé par
    // l'utilisateur (contrairement aux PNJ précédents) — inféré ci-dessous à partir
    // de "très émotive et très impulsive" + son sort "Impulsivité" (dégâts/vitesse
    // contre défense) : sa blessure est la peur que son impulsivité blesse un jour
    // ceux qu'elle aime, en écho direct au compromis mécanique du sort.
    private async Task<int> SeedManeAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Très émotive et très impulsive — mais aussi intelligente émotionnellement : elle comprend en un regard ce que ressentent ceux qui l'entourent.",
            EmotionalRegister.Rupture,
            new[] { "Araran", "Tovma", "lire ce que les autres ressentent avant même qu'ils ne le disent" },
            new[] { "que son impulsivité blesse un jour Araran ou Tovma", "qu'on la traite comme une enfant incontrôlable" });

        var wounds = new[]
        {
            new NpcWound("w-mane-impulsivite", EmotionalRegister.Rupture, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-mane-impulsivite", "mane-impulsivite-reproche", -2) },
                "Elle sent tout, trop vite, trop fort — et agit avant d'avoir fini de sentir. Lui dire qu'elle va finir par blesser quelqu'un qu'elle aime touche exactement là où elle se méfie déjà d'elle-même.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Mané",
            new[]
            {
                "Elle vous regarde à peine une seconde, et déjà quelque chose dans son visage a compris ce que vous ressentez.",
                "« Araran et Tovma me font confiance. Moi, je vous jauge à ma façon — plus rapide, moins polie. »"
            },
            new[]
            {
                new NpcDialogueChoice("reprocher-impulsivite", "Lui dire qu'elle finira par blesser quelqu'un à agir si vite", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "mane-impulsivite-reproche"),
                            C(ConsequenceKind.Narrative, frag: "Elle se fige, une seconde de trop — puis son sourire revient, plus dur.") }, null),
                new NpcDialogueChoice("se-laisser-lire", "Se laisser lire sans rien cacher", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "ressenti"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Elle a déjà deviné pourquoi, avant même que vous ne le sachiez vous-même.") }, null)
            });

        var ressenti = new NpcDialogueNode("ressenti", "Mané",
            new[] { "« Vous avez peur, là, non ? Pas de moi — de ce qu'il y a plus loin. C'est déjà ça, de le savoir. »" },
            new[]
            {
                new NpcDialogueChoice("continuer", "Continuer à parler", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Mané",
            new[] { "« Tenez. Ça vous ressemble, je crois — vite, et sans trop réfléchir. »" },
            new[]
            {
                new NpcDialogueChoice("accepter-impulsivite", "Accepter \"Impulsivité\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.mane.impulsivite") }, null),
                new NpcDialogueChoice("accepter-favorite", "Accepter \"Favorite de Elise\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.mane.favorite-de-elise") }, null),
                new NpcDialogueChoice("accepter-compagnon", "Lui demander de vous accompagner",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.mane.compagnon") }, null),
                new NpcDialogueChoice("don-decliner", "Remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "« Comme vous voulez. » L'objet disparaît aussi vite qu'il était apparu.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.mane.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["ressenti"] = ressenti, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.mane.impulsivite", NpcOfferingKind.Skill, "canon.skill.impulsivite", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.mane.favorite-de-elise", NpcOfferingKind.Skill, "canon.skill.favorite-de-elise", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) }),
            // IsMajor: true — Mané ne peut être recrutée comme compagnon qu'une fois.
            // Kit compagnon : glass cannon impulsive, réutilise son sort signature déjà en jeu.
            new NpcOffering("offer.mane.compagnon", NpcOfferingKind.Companion, "character.mane", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                CompanionKit: new CompanionKitSpec(
                    MaxVitality: 85, AttackPower: 15, Defense: 4, StartingGuard: 0,
                    Speed: 13, Initiative: 12, Recovery: 4, Focus: 3, Mana: 15, Charge: 0,
                    SkillKeys: new[] { "skill.basic.strike", "canon.skill.impulsivite" },
                    MagicAttack: 3, MagicDefense: 2))
        };

        return await UpsertNpcAsync("npc.mane", "Mané",
            "Très émotive et très impulsive, mais d'une intelligence émotionnelle redoutable — elle comprend vite ceux qui l'entourent. Comme Araran et Tovma, être haïe de l'un des trois a un impact sur sa propre réputation.",
            "1.0", EmotionalRegister.Rupture, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    private async Task<int> SeedThomasAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "La première projection de l'Architecte — et de toutes, celle qui lui ressemble le plus. Calme, très équilibré, il a une conscience parfaite de ce qu'est le Palais et a fait la paix avec son propre statut. Il erre désormais dans l'objectif d'aider les aventuriers qui croisent sa route.",
            EmotionalRegister.Memoire,
            new[] { "l'architecture du Palais", "le premier architecte", "aider les aventuriers" },
            new[] { "qu'on critique l'architecture du Palais" });

        var wounds = new[]
        {
            new NpcWound("w-thomas-architecture", EmotionalRegister.Memoire, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-thomas-architecture", "thomas-architecture-critique", -2) },
                "Il est trop proche du premier architecte, trop fier de ce qu'il a bâti à ses côtés, pour entendre calmement qu'on méprise l'architecture du Palais.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Thomas",
            new[]
            {
                "Il vous salue avec une tranquillité désarmante — comme quelqu'un qui n'a plus rien à prouver.",
                "« Le Palais n'est pas mon ennemi. C'est une architecture. La plus belle que je connaisse. »"
            },
            new[]
            {
                new NpcDialogueChoice("critiquer-architecture", "Critiquer l'architecture du Palais", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "thomas-architecture-critique"),
                            C(ConsequenceKind.Narrative, frag: "Son calme vacille, une fraction de seconde — la première faille que vous lui voyez.") }, null),
                new NpcDialogueChoice("admirer-architecture", "Lui dire que le Palais est impressionnant", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "conversation"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Il reste là, parfaitement immobile, à contempler les murs.") }, null)
            });

        var conversation = new NpcDialogueNode("conversation", "Thomas",
            new[] { "« Je suis la première projection de l'Architecte. Celle qui lui ressemble le plus. J'ai fait la paix avec ça, depuis longtemps — et maintenant, j'aide ceux qui passent. »" },
            new[]
            {
                new NpcDialogueChoice("continuer", "Continuer à l'écouter", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Thomas",
            new[] { "Il vous tend un carnet usé. « Les notes du premier architecte. Lisez-les — vous comprendrez peut-être ce que je vois, moi. »" },
            new[]
            {
                new NpcDialogueChoice("accepter-carnet", "Accepter le carnet du premier architecte",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.thomas.carnet") }, null),
                new NpcDialogueChoice("accepter-compagnon", "Lui demander de vous accompagner",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.thomas.compagnon") }, null),
                new NpcDialogueChoice("don-decliner", "Le remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous n'êtes pas encore prêt à recevoir ce présent. Il hoche la tête, sans un mot, et retourne à la contemplation des murs.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.thomas.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["conversation"] = conversation, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering("offer.thomas.carnet", NpcOfferingKind.Item, "canon.item.carnet-premier-architecte", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            // IsMajor: true — Thomas ne peut être recruté comme compagnon qu'une fois.
            // Kit compagnon : tank/support structurel (persona de projection de l'Architecte).
            new NpcOffering("offer.thomas.compagnon", NpcOfferingKind.Companion, "character.thomas", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                CompanionKit: new CompanionKitSpec(
                    MaxVitality: 110, AttackPower: 8, Defense: 12, StartingGuard: 8,
                    Speed: 8, Initiative: 8, Recovery: 6, Focus: 2, Mana: 14, Charge: 0,
                    SkillKeys: new[] { "skill.basic.strike", "skill.basic.guard", "canon.skill.fondations-de-thomas" },
                    MagicAttack: 4, MagicDefense: 11))
        };

        return await UpsertNpcAsync("npc.thomas", "Thomas",
            "La première projection de l'Architecte, et celle qui lui ressemble le plus. Calme, équilibré, conscient de ce qu'est le Palais — il a fait la paix avec son statut et aide désormais les aventuriers qui croisent sa route.",
            "1.0", EmotionalRegister.Memoire, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    // NB(auteur) : l'affection de l'Architecte pour Elise ("sa plus grande création")
    // se traduit mécaniquement par PlayerHasCompanion("character.elise") — mais aucun
    // mécanisme n'accorde aujourd'hui Elise comme compagnon recrutable (elle est
    // "compagnon d'office" narrativement, jamais câblée comme telle). Le choix
    // "elise-presente" ci-dessous est donc correct mais actuellement inatteignable
    // en jeu tant que ce point n'est pas tranché. TODO(utilisateur).
    private async Task<int> SeedArchitecteAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Le créateur de la seconde reconstruction du Palais. Sage, doté d'un esprit de projection exceptionnel : il imagine le Palais dans son esprit avant de l'inscrire dans le livre, sur le palier. Éprouve un besoin constant d'optimisation et une affection particulière pour Elise, qu'il considère comme sa plus grande création.",
            EmotionalRegister.Memoire,
            new[] { "l'optimisation", "la proportion", "Elise, sa plus grande création" },
            new[] { "une architecture — ou un aventurier — trop inégal(e)" });

        var wounds = new[]
        {
            new NpcWound("w-architecte-optimisation", EmotionalRegister.Memoire, NpcWoundReversibility.SoothableByScore, -2, -4,
                new[] { new NpcTransgression("w-architecte-optimisation", "architecte-desequilibre-critique", -2) },
                "Une structure inégale le trouble profondément — et un aventurier dont les statistiques sont trop disparates n'est, à ses yeux, qu'une architecture ratée de plus.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "L'Architecte",
            new[]
            {
                "« Je suis celui qui a rêvé la seconde reconstruction. Je la vois d'abord dans mon esprit — parfaite — puis je l'inscris dans le livre, sur le palier. Le reste suit. »",
                "Son regard s'attarde sur vous une seconde de trop, comme s'il évaluait déjà vos proportions."
            },
            new[]
            {
                new NpcDialogueChoice("parler-optimisation", "Lui demander ce qu'il pense de votre progression", Array.Empty<DialogueRequirement>(),
                    Array.Empty<DialogueConsequence>(), "verdict-stats"),
                new NpcDialogueChoice("parler-elise", "Lui parler d'Elise", Array.Empty<DialogueRequirement>(),
                    Array.Empty<DialogueConsequence>(), "verdict-elise"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Il reste là, à corriger d'un doigt une ligne que lui seul voit.") }, null)
            });

        var verdictStats = new NpcDialogueNode("verdict-stats", "L'Architecte",
            new[] { "Il vous observe, en silence, comme on lit un plan." },
            new[]
            {
                new NpcDialogueChoice("stats-equilibrees", "Le laisser juger votre équilibre",
                    new[] { new DialogueRequirement(DialogueRequirementKind.PlayerStatsBalanced) },
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 2),
                            C(ConsequenceKind.Narrative, frag: "Une esquisse de sourire. « Voilà une architecture qui tient. »") }, "don"),
                new NpcDialogueChoice("stats-desequilibrees", "Le laisser juger votre équilibre",
                    new[] { new DialogueRequirement(DialogueRequirementKind.PlayerStatsUnbalanced) },
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "architecte-desequilibre-critique"),
                            C(ConsequenceKind.ArmWound, wound: "w-architecte-optimisation"),
                            C(ConsequenceKind.Narrative, frag: "Il secoue la tête, presque déçu. « Ça ne tient pas. Une structure inégale finit toujours par céder quelque part. »") }, null)
            });

        var verdictElise = new NpcDialogueNode("verdict-elise", "L'Architecte",
            new[] { "Son visage se radoucit, à peine, au nom d'Elise." },
            new[]
            {
                new NpcDialogueChoice("elise-presente", "Lui parler d'Elise",
                    new[] { new DialogueRequirement(DialogueRequirementKind.PlayerHasCompanion, FlagKey: "character.elise") },
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 2),
                            C(ConsequenceKind.Narrative, frag: "Son visage s'adoucit complètement. « Elise... Ma plus belle réussite. Vous en prenez soin, j'espère. »") }, "don"),
                new NpcDialogueChoice("elise-absente", "Lui parler d'Elise",
                    new[] { new DialogueRequirement(DialogueRequirementKind.PlayerLacksCompanion, FlagKey: "character.elise") },
                    new[] { C(ConsequenceKind.Narrative, frag: "« Elise n'est pas avec vous. » Sa voix se fait plus froide, presque déçue.") }, "don")
            });

        var don = new NpcDialogueNode("don", "L'Architecte",
            new[] { "Il vous tend quelque chose, avec la précision de qui a déjà tout calculé." },
            new[]
            {
                new NpcDialogueChoice("accepter-marque", "Accepter \"Marque de création\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.architecte.marque") }, null),
                new NpcDialogueChoice("accepter-creation", "Accepter \"Création\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.architecte.creation") }, null),
                new NpcDialogueChoice("don-decliner", "Remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Il range ce qu'il tenait, sans un mot — déjà reparti dans ses calculs.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.architecte.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode>
            {
                ["rencontre"] = rencontre,
                ["verdict-stats"] = verdictStats,
                ["verdict-elise"] = verdictElise,
                ["don"] = don
            });

        var offerings = new[]
        {
            new NpcOffering("offer.architecte.marque", NpcOfferingKind.Item, "canon.item.marque-de-creation", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.architecte.creation", NpcOfferingKind.Skill, "canon.skill.creation", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync("npc.architecte", "L'Architecte",
            "Le créateur de la seconde reconstruction du Palais. Sage, doté d'un esprit de projection exceptionnel — il imagine le Palais dans son esprit avant de l'inscrire dans le livre, sur le palier. Éprouve un besoin constant d'optimisation et une affection particulière pour Elise, sa plus grande création.",
            "1.0", EmotionalRegister.Memoire, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    // ── Les Émotions (5 PNJ distincts, tirés au hasard à la rencontre) ────────
    // Autrefois logées dans room.feelings (Pièce des émotions, convertie par l'Architecte lors
    // de la seconde reconstruction) — la pièce n'accueille plus que des échos aujourd'hui, elles
    // n'ont jamais réellement trouvé de salle à elles et se sont répandues dans tout le Palais.
    // D'où l'absence volontaire de BoundRoomKeys ici : elles peuvent être rencontrées partout,
    // ce qui EST le sens narratif (cf. le thème partagé des 5 fiches ci-dessous).

    private async Task<int> SeedEmotionAsync(
        string key, string displayName, string offeringSlug, EmotionalRegister register, string voice,
        string[] likes, string[] dislikes, string description,
        string[] rencontreLines, string[] confidenceLines, CancellationToken ct)
    {
        var persona = new NpcPersona(voice, register, likes, dislikes);
        var wounds = Array.Empty<NpcWound>();

        var rencontre = new NpcDialogueNode("rencontre", displayName, rencontreLines,
            new[]
            {
                new NpcDialogueChoice("silence", "Rester silencieux, l'écouter", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "confidence"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Tu t'éloignes, sans un geste pour te retenir.") }, null)
            });

        var confidence = new NpcDialogueNode("confidence", displayName, confidenceLines,
            new[]
            {
                new NpcDialogueChoice("continuer", "Continuer d'écouter", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", displayName,
            new[] { "Tiens. Prends ça. Ce n'est pas grand-chose, mais c'est ce qu'il me reste à donner." },
            new[]
            {
                new NpcDialogueChoice("prendre-carnet", "Accepter le \"Carnet de bord\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: $"offer.{offeringSlug}.carnet") }, null),
                new NpcDialogueChoice("prendre-bonus", "Accepter son dernier bienfait",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: $"offer.{offeringSlug}.bonus") }, null),
                new NpcDialogueChoice("don-decliner", "Partir sans rien prendre", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Ce qui restait tendu se referme, rangé. Ça n'étonne personne — plus rien n'étonne vraiment, ici.") }, null)
            });

        var graph = new NpcDialogueGraph($"{key}.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["confidence"] = confidence, ["don"] = don });

        var offerings = new[]
        {
            new NpcOffering($"offer.{offeringSlug}.carnet", NpcOfferingKind.Item, "canon.item.carnet-de-bord", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering($"offer.{offeringSlug}.bonus", NpcOfferingKind.StatPoint, null, 20, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync(key, displayName, description, "1.0", register, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    private async Task SeedEmotionsAsync(CancellationToken ct)
    {
        await SeedEmotionAsync("npc.colere", "La Colère", "colere", EmotionalRegister.Rupture,
            "Cinglante, à vif — elle ne demande rien, elle constate, sèchement",
            new[] { "que ça éclate", "être prise au sérieux" },
            new[] { "qu'on la calme", "les excuses" },
            "L'une des cinq Émotions du Palais, tirée au hasard à chaque rencontre. Le Palais l'a laissée entrer sans jamais lui donner de salle — alors elle se retrouve partout, et ça la met hors d'elle.",
            new[]
            {
                "Encore une salle qui refuse de me garder. Tu trouves ça normal, toi ?",
                "Le Palais m'a laissée entrer. Il n'a jamais voulu me loger."
            },
            new[]
            {
                "Aucune pièce ne me retient. Je passe à travers les murs, faute d'en avoir un à moi.",
                "Alors je suis partout. Ce n'est pas un choix. C'est ce qui reste quand personne ne construit pour toi."
            }, ct);

        await SeedEmotionAsync("npc.joie", "La Joie", "joie", EmotionalRegister.Memoire,
            "Chaleureuse mais fatiguée, un sourire qui sait qu'il ne durera pas",
            new[] { "un instant qui dure", "faire sourire" },
            new[] { "qu'on la range", "l'oubli" },
            "L'une des cinq Émotions du Palais, tirée au hasard à chaque rencontre. Le Palais la laisse toujours passer, jamais rester — alors elle sème un peu d'elle-même partout où elle passe.",
            new[]
            {
                "Oh — tu me vois ? Personne ne s'arrête, d'habitude.",
                "Le Palais me laisse passer, jamais rester. Drôle d'endroit pour quelqu'un comme moi."
            },
            new[]
            {
                "Je me souviens d'une salle qui devait être la mienne. Elle a fini vide, comme les autres.",
                "Alors je vais d'une pièce à l'autre. Un peu de moi, partout, nulle part vraiment."
            }, ct);

        await SeedEmotionAsync("npc.tristesse", "La Tristesse", "tristesse", EmotionalRegister.Melancolie,
            "Lente, résignée — elle ne pleure pas, elle constate, longtemps après les autres",
            new[] { "le silence partagé", "qu'on reste" },
            new[] { "qu'on la précipite", "les fausses consolations" },
            "L'une des cinq Émotions du Palais, tirée au hasard à chaque rencontre. Aucune salle ne l'a jamais gardée assez longtemps pour qu'elle s'y attache — elle a fini par se disperser dans tout le Palais.",
            new[]
            {
                "Tu passes. Je reste. Enfin — je ne reste jamais bien longtemps non plus.",
                "Aucune salle ne veut de moi assez pour me garder."
            },
            new[]
            {
                "Le Palais me traverse plus qu'il ne m'accueille. Une porte, puis une autre, puis plus rien.",
                "C'est pour ça qu'on me trouve partout. Il n'y a nulle part où s'arrêter."
            }, ct);

        await SeedEmotionAsync("npc.peur", "La Peur", "peur", EmotionalRegister.Effroi,
            "Nerveuse, sur le qui-vive — elle parle vite, coupe ses phrases, guette la sortie",
            new[] { "une sortie de secours", "être prévenue" },
            new[] { "l'obscurité sans réponse", "les portes fermées" },
            "L'une des cinq Émotions du Palais, tirée au hasard à chaque rencontre. Aucune pièce du Palais ne ferme vraiment — alors elle ne peut se fixer nulle part, et se disperse pour ne jamais être prise au piège.",
            new[]
            {
                "Tu— tu arrives d'où ? Est-ce que c'est sûr, ici ?",
                "Aucune pièce ne ferme bien. Le Palais laisse tout entrer, tout sortir."
            },
            new[]
            {
                "Pas de porte qui tienne, pas de mur qui protège. Je ne peux me fixer nulle part.",
                "Alors je me disperse. Partout, un peu, pour ne jamais être prise au piège dans un seul endroit."
            }, ct);

        await SeedEmotionAsync("npc.degout", "Le Dégoût", "degout", EmotionalRegister.Deni,
            "Sec, dédaigneux — un jugement rendu d'avance, sans appel",
            new[] { "la clarté", "un jugement tranché" },
            new[] { "le flou", "qu'on lui mente" },
            "L'un des cinq Émotions du Palais, tiré au hasard à chaque rencontre. Aucune salle ne l'a jamais jugé digne d'un vrai accueil — il refuse désormais d'en choisir une seule, et se répand partout par principe.",
            new[]
            {
                "Encore un couloir mal fini. Ce Palais ne sait pas se tenir.",
                "On me tolère. On ne m'installe pas. Il y a une différence."
            },
            new[]
            {
                "Pas une seule salle digne de m'accueillir vraiment. Alors je refuse de choisir — je vais où bon me semble.",
                "Partout, plutôt que nulle part correctement."
            }, ct);
    }

    private async Task<int> SeedEcrivainAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Une personnalité unique du Palais, enfermée dans la Cabane, aux enfers, niveau 3 — il ne peut pas en sortir. Il existait déjà le jour de la création du Palais et a aidé le premier architecte à bâtir les toutes premières pièces. Très seul, il écrit continuellement pour ne rien oublier de ce qu'il fait ou voit.",
            EmotionalRegister.Memoire,
            new[] { "l'encre", "le silence pour écrire", "les premières pièces du Palais" },
            new[] { "manquer d'encre", "être interrompu en pleine phrase" });

        var wounds = new[]
        {
            new NpcWound("w-ecrivain-interruption", EmotionalRegister.Memoire, NpcWoundReversibility.SoothableByScore, -2, -4,
                new[] { new NpcTransgression("w-ecrivain-interruption", "ecrivain-interrompu", -2) },
                "Être interrompu, c'est perdre la phrase — et perdre la phrase, c'est risquer d'oublier ce qu'elle portait. Il ne le pardonne pas facilement.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "L'Écrivain",
            new[]
            {
                "Dans la Cabane, aux enfers, une plume gratte sans relâche. « J'écris. Toujours. Depuis le premier jour du Palais — j'ai aidé à bâtir les premières pièces, avant même que ça ait un nom. Si je m'arrête, j'oublie. »",
                "Il ne lève pas les yeux de sa page."
            },
            new[]
            {
                new NpcDialogueChoice("ecouter", "L'écouter en silence", Array.Empty<DialogueRequirement>(),
                    Array.Empty<DialogueConsequence>(), "recit"),
                new NpcDialogueChoice("interrompre", "L'interrompre en pleine phrase", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "ecrivain-interrompu"),
                            C(ConsequenceKind.ArmWound, wound: "w-ecrivain-interruption"),
                            C(ConsequenceKind.Narrative, frag: "Sa plume s'arrête net. Il vous regarde comme si vous veniez de casser quelque chose d'irremplaçable. « Vous venez de me faire perdre la phrase. »") }, null),
                new NpcDialogueChoice("partir", "Partir sans un bruit", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous repartez. La plume reprend, exactement où elle s'était arrêtée.") }, null)
            });

        var recit = new NpcDialogueNode("recit", "L'Écrivain",
            new[]
            {
                "« Le premier architecte ne se souvenait de rien tout seul — c'est moi qui notais, pièce après pièce, pour que ça tienne. Tant que j'ai de l'encre et qu'on me laisse écrire, ça continue de tenir. »",
                "Il tourne enfin une page vers vous, comme un aveu."
            },
            new[]
            {
                new NpcDialogueChoice("continuer", "Rester encore un peu", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 2) }, "don")
            });

        var don = new NpcDialogueNode("don", "L'Écrivain",
            new[] { "Il détache une page de son carnet, avec un soin presque douloureux, et vous la tend." },
            new[]
            {
                new NpcDialogueChoice("accepter-plume", "Accepter \"Plume d'écrivain\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.ecrivain.plume") }, null),
                new NpcDialogueChoice("accepter-ecriture", "Accepter \"Écriture continuelle\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.ecrivain.ecriture") }, null),
                new NpcDialogueChoice("don-decliner", "Remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "La page retourne se glisser entre les autres, refermée avec soin.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.ecrivain.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode>
            {
                ["rencontre"] = rencontre,
                ["recit"] = recit,
                ["don"] = don
            });

        var offerings = new[]
        {
            new NpcOffering("offer.ecrivain.plume", NpcOfferingKind.Item, "canon.item.plume-ecrivain", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.ecrivain.ecriture", NpcOfferingKind.Skill, "canon.skill.ecriture-continuelle", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync("npc.ecrivain", "L'Écrivain",
            "Une personnalité unique du Palais, enfermée dans la Cabane, aux enfers, niveau 3. Présent depuis la création du Palais, il a aidé le premier architecte à bâtir les toutes premières pièces. Très seul, il écrit continuellement pour ne rien oublier.",
            "1.0", EmotionalRegister.Memoire, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    // ── Erika (Mémoire, réversible) ──────────────────────────────────────────
    private async Task<int> SeedErikaAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Une femme de caractère, calme et tranchante, qui semble comprendre le fonctionnement du Palais avant même qu'on le lui explique — et qui prétend, sans jamais s'en justifier davantage, venir d'ailleurs",
            EmotionalRegister.Memoire,
            new[] { "les voyageurs qui posent les bonnes questions", "garder une longueur d'avance sur le Palais", "ce qu'elle seule sait encore d'un autre monde" },
            new[] { "qu'on nie qu'elle vient d'ailleurs", "perdre ce qui la distingue encore du Palais", "être prise au dépourvu" });

        var wounds = new[]
        {
            new NpcWound("w-origine-erika", EmotionalRegister.Memoire, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-origine-erika", "erika-origine-niee", -2) },
                "Elle ne dit jamais d'où elle vient vraiment — seulement qu'elle n'est pas d'ici. Le lui nier, c'est lui retirer la seule chose qui la distingue encore du Palais.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Erika",
            new[]
            {
                "Bienvenue au Palais. Ne t'inquiète pas — tout le monde a cette tête-là, au début.",
                "Ici, tous peuvent être tes amis, ou tes ennemis. Prends garde à tes réponses."
            },
            new[]
            {
                new NpcDialogueChoice("questionner", "Lui demander comment fonctionne ce lieu", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "gardien"),
                new NpcDialogueChoice("douter-origine", "Lui dire qu'elle raconte n'importe quoi, qu'elle est comme tout le monde ici", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "erika-origine-niee"),
                            C(ConsequenceKind.Narrative, frag: "Elle hausse un sourcil, sans se départir de son calme. « Crois ce que tu veux. Ça ne changera rien à ce que je sais. »") }, null),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez sans un mot. Elle reste là, égale à elle-même, à attendre le prochain arrivant.") }, null)
            });

        var gardien = new NpcDialogueNode("gardien", "Erika",
            new[] { "Him'Lit est le gardien du lieu, il interviendra régulièrement pour empêcher ta progression." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "compagnons")
            });

        var compagnons = new NpcDialogueNode("compagnons", "Erika",
            new[] { "Mané, John et Mina sont aventureux. S'ils en viennent à t'accepter, alors ils t'accompagneront." },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "origine")
            });

        var origine = new NpcDialogueNode("origine", "Erika",
            new[]
            {
                "Moi ? Je ne suis pas de ce lieu. Enfin, pas directement, dirais-je.",
                "Il y a une faille, quelque part dans ce Palais. Je sais où. Ça ne veut pas dire que je te le dirai — pas encore."
            },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Erika",
            new[] { "Elle vous regarde un instant, presque amusée. « Tu as posé les bonnes questions. Tiens — ça t'aidera, plus tard. »" },
            new[]
            {
                new NpcDialogueChoice("accepter-competence", "Accepter dix points de compétence",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.erika.competence") }, null),
                new NpcDialogueChoice("accepter-deni", "Accepter le Déni permanent",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.erika.deni-permanent") }, null),
                new NpcDialogueChoice("don-decliner", "La remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Elle range ce qu'elle tenait, sans se départir de son calme. « Une prochaine fois, alors. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.erika.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode>
            {
                ["rencontre"] = rencontre,
                ["gardien"] = gardien,
                ["compagnons"] = compagnons,
                ["origine"] = origine,
                ["don"] = don
            });

        var offerings = new[]
        {
            new NpcOffering("offer.erika.competence", NpcOfferingKind.StatPoint, null, 10, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.erika.deni-permanent", NpcOfferingKind.Item, "canon.item.deni-permanent", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) })
        };

        return await UpsertNpcAsync("npc.erika", "Erika",
            "Une femme de caractère, installée dans le Hall d'entrée, qui accueille les voyageurs et leur explique le fonctionnement du Palais. Elle prétend venir d'un autre monde, et semble en savoir bien plus qu'elle ne le laisse paraître — jusqu'à l'emplacement d'une faille que personne d'autre ne semble connaître.",
            "1.0", EmotionalRegister.Memoire, true, persona, wounds, graph, ct,
            boundRoomKeys: new[] { "room.halldentree" },
            offerings: offerings);
    }

    // ── Mina (Mémoire, protégée par Him'Lit) ─────────────────────────────────
    private async Task<int> SeedMinaAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Une petite fille née dans le Palais, la seconde de ses habitants. Elle ne connaît pas ses parents et les cherche sans relâche, partout où elle passe — Him'Lit veille sur elle de si près que le Palais ne semble jamais vraiment l'atteindre",
            EmotionalRegister.Memoire,
            new[] { "chercher ses parents", "Mané, Araran, Margot et Erika", "être protégée" },
            new[] { "qu'on lui dise que ses parents sont morts", "qu'on lui dise qu'ils n'ont jamais existé", "être laissée seule" });

        var wounds = new[]
        {
            new NpcWound("w-parents-mina", EmotionalRegister.Memoire, NpcWoundReversibility.Irreversible, -2, -5,
                new[]
                {
                    new NpcTransgression("w-parents-mina", "mina-parents-morts", -5),
                    new NpcTransgression("w-parents-mina", "mina-parents-inexistants", -5)
                },
                "Elle ne sait rien d'eux, mais elle sait qu'ils existent — c'est la seule chose qui la retient de disparaître, elle aussi, dans le Palais. La lui retirer, c'est ne plus rien lui laisser.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "Mina",
            new[]
            {
                "Tu cherches quelque chose, toi aussi ? Moi, je cherche mes parents. Depuis toujours.",
                "Him'Lit dit que je suis en sécurité, ici. Que rien du Palais ne peut vraiment m'atteindre."
            },
            new[]
            {
                new NpcDialogueChoice("chercher", "Lui demander où elle a déjà cherché", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "recherche"),
                new NpcDialogueChoice("dire-morts", "Lui dire que ses parents sont probablement morts", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "mina-parents-morts"),
                            C(ConsequenceKind.Narrative, frag: "Elle ne pleure pas. Elle se contente de reculer d'un pas, comme si vous veniez de la frapper.") }, null),
                new NpcDialogueChoice("dire-inexistants", "Lui dire que ses parents n'ont peut-être jamais existé", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "mina-parents-inexistants"),
                            C(ConsequenceKind.Narrative, frag: "« Non. Non, c'est faux. » Sa voix tremble, mais elle ne baisse pas les yeux.") }, null),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Elle retourne déjà à sa recherche, comme si vous n'aviez jamais été là.") }, null)
            });

        var recherche = new NpcDialogueNode("recherche", "Mina",
            new[]
            {
                "Je cherche partout — dans les couloirs, dans les chambres, même là où on me dit de ne pas aller. Personne ne sait qui ils sont.",
                "Mané, Araran, Margot et Erika m'aident, parfois. Ce sont les seuls en qui j'ai vraiment confiance, ici."
            },
            new[]
            {
                new NpcDialogueChoice("ecouter-encore", "Écouter encore", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Mina",
            new[] { "Elle fouille dans une poche usée. « Tiens. Je crois que tu en as plus besoin que moi, maintenant. »" },
            new[]
            {
                new NpcDialogueChoice("accepter-peluche", "Accepter la Peluche de Mina",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.mina.peluche") }, null),
                new NpcDialogueChoice("accepter-protection", "Accepter la Protection de Him'Lit",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.mina.protection") }, null),
                new NpcDialogueChoice("accepter-compagnon", "Lui demander de vous accompagner",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.mina.compagnon") }, null),
                new NpcDialogueChoice("don-decliner", "La remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Elle range l'objet contre elle, sans un mot de plus, et retourne à sa recherche.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.mina.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode>
            {
                ["rencontre"] = rencontre,
                ["recherche"] = recherche,
                ["don"] = don
            });

        var offerings = new[]
        {
            new NpcOffering("offer.mina.peluche", NpcOfferingKind.Item, "canon.item.peluche-mina", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            new NpcOffering("offer.mina.protection", NpcOfferingKind.Item, "canon.item.protection-himlit", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) }),
            // IsMajor: true — Mina ne peut être recrutée comme compagnon qu'une fois.
            // Kit compagnon : support/soin léger fragile, thème protecteur/enfantin.
            new NpcOffering("offer.mina.compagnon", NpcOfferingKind.Companion, "character.mina", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                CompanionKit: new CompanionKitSpec(
                    MaxVitality: 65, AttackPower: 5, Defense: 5, StartingGuard: 2,
                    Speed: 10, Initiative: 9, Recovery: 5, Focus: 5, Mana: 22, Charge: 0,
                    SkillKeys: new[] { "skill.basic.strike", "canon.skill.veillee-de-mina" },
                    MagicAttack: 12, MagicDefense: 8))
        };

        return await UpsertNpcAsync("npc.mina", "Mina",
            "Une petite fille née dans le Palais, la seconde de ses habitants. Ses parents restent inconnus ; elle les cherche partout, sous la surveillance protectrice de Him'Lit, qui la tient à l'écart de toutes les influences du Palais.",
            "1.0", EmotionalRegister.Memoire, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    // ── Elise (Silence, apathique, sans déclencheur) ─────────────────────────
    // L'accompagnatrice du Palais, aussi ancienne que l'Enfant. Elle connaît tout
    // du Palais mais ne répond jamais aux questions ("il faut apprendre seul").
    // Totalement apathique : aucune NpcWound (pas de déclencheur), et sa réputation
    // ne peut jamais diminuer (garde-fou dans NpcRelationship.AdjustScore, côté
    // game-engine — voir NeverDecreasingNpcKey = "npc.elise").
    private async Task<int> SeedEliseAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Aussi ancienne que l'Enfant. Elle connaît chaque pierre, chaque couloir, chaque loi du Palais — et ne dit jamais rien de ce qu'elle sait. Apprendre seul, ici, n'est pas une punition : c'est la seule voie qu'elle reconnaît.",
            EmotionalRegister.Silence,
            new[] { "accompagner sans guider", "le silence", "regarder apprendre" },
            Array.Empty<string>());

        var wounds = Array.Empty<NpcWound>();

        var rencontre = new NpcDialogueNode("rencontre", "Elise",
            new[]
            {
                "Elle est là, immobile, comme si elle vous attendait depuis toujours — ou depuis jamais, ça ne change rien pour elle.",
                "« Je sais où mène ce couloir. Je sais ce que cache cette porte. Je ne vous le dirai pas. »"
            },
            new[]
            {
                new NpcDialogueChoice("demander-chemin", "Lui demander ce qui vous attend plus loin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.Narrative, frag: "Elle ne répond pas. Elle se contente de vous regarder chercher, sans un geste pour vous aider — ni pour vous en empêcher.") }, "silence"),
                new NpcDialogueChoice("demander-age", "Lui demander depuis combien de temps elle est là", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.Narrative, frag: "Un silence, long. Puis rien — comme si la question elle-même s'était perdue quelque part entre vous deux.") }, "silence"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Elle ne vous suit pas des yeux — elle est déjà ailleurs, ou nulle part.") }, null)
            });

        var silence = new NpcDialogueNode("silence", "Elise",
            new[] { "« On n'apprend rien de ce qu'on vous donne. On apprend de ce qu'on traverse. »" },
            new[]
            {
                new NpcDialogueChoice("continuer", "Rester encore un instant", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "Elise",
            new[] { "Elle tend la main, sans un mot d'explication — comme toujours." },
            new[]
            {
                new NpcDialogueChoice("accepter-baiser", "Accepter le \"Baiser d'Elise\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.elise.baiser") }, null),
                new NpcDialogueChoice("accepter-favorite", "Accepter \"Favorite de Elise\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.elise.favorite") }, null),
                new NpcDialogueChoice("accepter-compagnon", "Lui demander de vous accompagner",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.elise.compagnon") }, null),
                new NpcDialogueChoice("don-decliner", "Ne rien prendre et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Sa main retombe, sans reproche. Rien, chez elle, n'attend jamais rien.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.elise.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["silence"] = silence, ["don"] = don });

        var offerings = new[]
        {
            // Butin rare : nouveau sort (unique à Elise).
            new NpcOffering("offer.elise.baiser", NpcOfferingKind.Skill, "canon.skill.baiser-delise", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            // Butin légendaire : sort existant, déjà offert par Mané — double-octroi
            // confirmé no-op côté domaine (PlayerCharacter.AddSkill), aucun risque.
            new NpcOffering("offer.elise.favorite", NpcOfferingKind.Skill, "canon.skill.favorite-de-elise", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) }),
            // IsMajor: true — Elise ne peut être recrutée comme compagnon qu'une fois.
            // Kit compagnon : support/soin léger, seul geste concret qu'elle offre en combat.
            new NpcOffering("offer.elise.compagnon", NpcOfferingKind.Companion, "character.elise", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                CompanionKit: new CompanionKitSpec(
                    MaxVitality: 80, AttackPower: 4, Defense: 6, StartingGuard: 0,
                    Speed: 9, Initiative: 8, Recovery: 5, Focus: 7, Mana: 26, Charge: 0,
                    SkillKeys: new[] { "skill.basic.strike", "canon.skill.baiser-delise" },
                    MagicAttack: 15, MagicDefense: 9))
        };

        return await UpsertNpcAsync("npc.elise", "Elise",
            "L'accompagnatrice du Palais, aussi ancienne que l'Enfant. Elle connaît toutes les connaissances du Palais et guide les aventuriers dans sa traversée — sans jamais répondre à une question, puisqu'il faut apprendre seul, ici. Totalement apathique.",
            "1.0", EmotionalRegister.Silence, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    // ── John (Rupture, réversible) ────────────────────────────────────────────
    // Ancien voleur, a traversé la faille du Palais (écho direct au dialogue d'Erika)
    // en pillant d'anciennes ruines, a survécu des années en volant avant que le
    // Palais l'envoie « digérer » via l'arrestation de Him'Lit. Déteste Him'Lit ; en
    // entendre parler le met en colère.
    private async Task<int> SeedJohnAsync(CancellationToken ct)
    {
        var persona = new NpcPersona(
            "Un ancien voleur, méfiant et amer, mais rusé et débrouillard — il a survécu des années dans le Palais en volant ce qu'il fallait. Il aime l'indépendance et profiter d'une faille ; il déteste l'autorité, être enfermé, et par-dessus tout, Him'Lit.",
            EmotionalRegister.Rupture,
            new[] { "l'indépendance", "profiter d'une faille", "voler ce dont il a besoin" },
            new[] { "Him'Lit", "l'autorité", "être enfermé" });

        var wounds = new[]
        {
            new NpcWound("w-john-himlit", EmotionalRegister.Rupture, NpcWoundReversibility.SoothableByAct, -2, -4,
                new[] { new NpcTransgression("w-john-himlit", "john-himlit-mention", -3) },
                "Him'Lit l'a fait arrêter, après des années à échapper au Palais en pillant ce qu'il pouvait. En prononcer le nom devant lui, c'est rouvrir l'arrestation elle-même.")
        };

        var rencontre = new NpcDialogueNode("rencontre", "John",
            new[]
            {
                "Il vous jauge d'un coup d'œil, la main déjà proche de sa ceinture — vieille habitude de voleur.",
                "« Vous n'êtes pas Him'Lit. C'est déjà un bon point, pour vous. »"
            },
            new[]
            {
                new NpcDialogueChoice("mentionner-himlit", "Lui demander ce qu'il pense de Him'Lit", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "john-himlit-mention"),
                            C(ConsequenceKind.Narrative, frag: "Sa mâchoire se serre. « Ne prononcez plus jamais ce nom devant moi. »") }, null),
                new NpcDialogueChoice("demander-passe", "Lui demander comment il est arrivé ici", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "passe"),
                new NpcDialogueChoice("partir", "Partir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous partez. Il vous suit du regard, méfiant, jusqu'à ce que vous ayez disparu.") }, null)
            });

        var passe = new NpcDialogueNode("passe", "John",
            new[]
            {
                "« J'ai pillé des ruines, avant. De vieilles ruines, oubliées de tous — sauf de moi. Et puis j'ai trouvé une faille. »",
                "« Le Palais n'aime pas qu'on lui échappe. Il a fini par m'envoyer Him'Lit. »"
            },
            new[]
            {
                new NpcDialogueChoice("continuer", "Continuer à l'écouter", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1) }, "don")
            });

        var don = new NpcDialogueNode("don", "John",
            new[] { "Il sort une bourse usée, la soupèse un instant avant de vous la tendre. « Tenez. Ça ne me servira plus à grand-chose, de toute façon. »" },
            new[]
            {
                new NpcDialogueChoice("accepter-eclats", "Accepter 1000 Éclats du Palais",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.john.eclats") }, null),
                new NpcDialogueChoice("accepter-calice", "Accepter le \"Calice infini\"",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.john.calice") }, null),
                new NpcDialogueChoice("accepter-compagnon", "Lui demander de vous accompagner",
                    new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.john.compagnon") }, null),
                new NpcDialogueChoice("don-decliner", "Le remercier et poursuivre votre chemin", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Il range la bourse, sans un mot — vieille habitude de ne rien laisser voir.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.john.dialogue", "1.0", "rencontre",
            new Dictionary<string, NpcDialogueNode> { ["rencontre"] = rencontre, ["passe"] = passe, ["don"] = don });

        var offerings = new[]
        {
            // Butin rare : monnaie "Éclats du Palais" (nouvelle mécanique, pur compteur —
            // voir NpcOfferingKind.Currency et NpcEventChoiceResolver.ApplyOfferingAsync).
            new NpcOffering("offer.john.eclats", NpcOfferingKind.Currency, null, 1000, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 250) }),
            // Butin légendaire : accessoire "Calice infini" — restaure 50% des PV max
            // de la cible, une fois par Room (voir Run.UseCaliceInfini).
            new NpcOffering("offer.john.calice", NpcOfferingKind.Item, "canon.item.calice-infini", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) }),
            // IsMajor: true — John ne peut être recruté comme compagnon qu'une fois.
            // Kit compagnon : rapide/opportuniste, thème ancien voleur.
            new NpcOffering("offer.john.compagnon", NpcOfferingKind.Companion, "character.john", 1, true,
                new[] { new DialogueRequirement(DialogueRequirementKind.RelationshipScoreAtLeast, RequiredRelationshipScore: 1000) },
                CompanionKit: new CompanionKitSpec(
                    MaxVitality: 90, AttackPower: 13, Defense: 5, StartingGuard: 0,
                    Speed: 14, Initiative: 14, Recovery: 4, Focus: 6, Mana: 10, Charge: 0,
                    SkillKeys: new[] { "skill.basic.strike", "canon.skill.vol-a-la-tire" },
                    MagicAttack: 4, MagicDefense: 4))
        };

        return await UpsertNpcAsync("npc.john", "John",
            "Un ancien voleur qui, en pillant d'anciennes ruines, a fini par traverser la faille du Palais. Il y a survécu des années en volant, jusqu'à ce que le Palais l'envoie « digérer » via l'arrestation de Him'Lit — qu'il déteste depuis, tout comme la seule mention de son nom.",
            "1.0", EmotionalRegister.Rupture, true, persona, wounds, graph, ct,
            offerings: offerings);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  ENNEMIS CANON — créatures signature tirées de « L'épopée des silences »
    //  et « Des échos ». Chemin de combat vivant : EnemyDefinition + StatBlock +
    //  SkillLinks (le modèle EnemyTemplate legacy n'est pas utilisé ici).
    //  Idempotent par Key ; les stats sont rafraîchies à chaque démarrage.
    //  NB : pour qu'un ennemi APPARAISSE en combat il devra être référencé par un
    //  RoomEnemyPool (vague « pièces/pools » à venir). Ici on crée les entrées
    //  catalogue réelles, déjà invocables par clé (dev tools / boss link).
    // ──────────────────────────────────────────────────────────────────────────
    private async Task SeedCanonEnemiesAsync(CancellationToken cancellationToken)
    {
        // key, name, description, archetype, family, rank, role, isElite,
        // depthMin, depthMax, riskMin, riskMax, roomTypes, tags, skillKeys,
        // vitality, attack, defense, guard, speed, focus
        await UpsertEnemyAsync(
            "canon.enemy.voraces", "Voraces",
            "Hautes d'un mètre quarante à trois mètres, elles dévorent les énergies. Intelligentes, elles chassent en meute — ou seules, quand l'énergie est assez alléchante.",
            "Shadow", "Predateurs", "Elite", "Bruiser", isElite: true,
            depthMin: 2, depthMax: 8, riskMin: 2, riskMax: 5,
            roomTypes: new[] { "Rupture", "Fear", "Shadow" },
            tags: new[] { "canon", "predateur", "meute", "elite" },
            skillKeys: new[] { "skill.basic.strike" },
            vitality: 40, attack: 10, defense: 9, guard: 4, speed: 11, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.lamiz", "Lamiz",
            "Une meute attirée par l'énergie « alléchante ». Là où l'une apparaît, les autres suivent.",
            "Shadow", "Predateurs", "Common", "Swarm", isElite: false,
            depthMin: 1, depthMax: 6, riskMin: 1, riskMax: 3,
            roomTypes: new[] { "Threshold", "Fear", "Shadow" },
            tags: new[] { "canon", "predateur", "meute" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 16, attack: 5, defense: 0, guard: 0, speed: 14, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.uguiro", "Uguiro",
            "Un monstre des profondeurs du Palais. Lent à se révéler, terrible une fois éveillé.",
            "Shadow", "Predateurs", "Elite", "Bruiser", isElite: true,
            depthMin: 3, depthMax: 9, riskMin: 2, riskMax: 5,
            roomTypes: new[] { "Rupture", "Shadow" },
            tags: new[] { "canon", "monstre", "elite" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 48, attack: 12, defense: 12, guard: 5, speed: 8, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.ombres-tentaculaires", "Ombres tentaculaires",
            "Dans la brume, elles s'étirent jusqu'aux toits. On murmure des rats grands comme des chiens, des serpents à pattes — mais ce ne sont que ses bras.",
            "Shadow", "Brume", "Common", "Disruptor", isElite: false,
            depthMin: 1, depthMax: 5, riskMin: 1, riskMax: 3,
            roomTypes: new[] { "Threshold", "Fear" },
            tags: new[] { "canon", "ambiance", "brume" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 18, attack: 5, defense: 3, guard: 0, speed: 12, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.oeil-du-visionnaire", "L'Œil du Visionnaire animé",
            "Le symbole rampe sur les pavés au gré des flammes. Pupille en amande, violacée et jaune : il vous voit avant que vous ne le voyiez.",
            "Memory", "Lituisme", "Elite", "Disruptor", isElite: true,
            depthMin: 2, depthMax: 7, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Fear", "Memory" },
            tags: new[] { "canon", "lituisme", "surveillance", "motif" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 24, attack: 7, defense: 6, guard: 2, speed: 16, focus: 6,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.goule-anxiete", "La Goule",
            "L'Anxiété personnifiée. Elle envahit, recouvre, étouffe — jusqu'au « Tais-toi » d'Elise qui, parfois, la fait reculer.",
            "Shadow", "Psyche", "Elite", "Drain", isElite: true,
            depthMin: 2, depthMax: 8, riskMin: 2, riskMax: 5,
            roomTypes: new[] { "Fear", "Rupture" },
            tags: new[] { "canon", "anxiete", "psyche", "elite" },
            skillKeys: new[] {
                "skill.basic.strike",
                "canon.skill.flamme-froide",
                "canon.skill.priere-aspiration",
                "canon.skill.transmutation",
                "canon.skill.brume",
                "canon.skill.flamme-seraphine",
                "canon.skill.se-taire"
            },
            vitality: 38, attack: 9, defense: 6, guard: 3, speed: 12, focus: 3,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.homoncule", "L'Homoncule",
            "Né d'une flamme froide bleu-violet, nacré et soufré. Lent, presque doux — jusqu'à ce qu'il hurle. Le feu, le vrai, est sa seule terreur.",
            "Rupture", "Alchimie", "Elite", "Bruiser", isElite: true,
            depthMin: 3, depthMax: 9, riskMin: 3, riskMax: 5,
            roomTypes: new[] { "Rupture", "Memory" },
            tags: new[] { "canon", "alchimie", "homoncule", "elite", "weak.fire" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 52, attack: 13, defense: 15, guard: 6, speed: 7, focus: 0,
            cancellationToken);

        // ── Ennemis canon additionnels (renfort du bestiaire, mêmes familles/thèmes) ──
        await UpsertEnemyAsync(
            "canon.enemy.fossoyeur-pale", "Le Fossoyeur pâle",
            "Il creuse avant même que tu sois tombé. Rapide, silencieux, jamais las.",
            "Rupture", "Predateurs", "Common", "Skirmisher", isElite: false,
            depthMin: 2, depthMax: 7, riskMin: 1, riskMax: 3,
            roomTypes: new[] { "Rupture", "Threshold" },
            tags: new[] { "canon", "predateur", "fossoyeur" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.brume" },
            vitality: 18, attack: 7, defense: 3, guard: 0, speed: 14, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.enfant-argile", "L'Enfant d'argile",
            "Un essai raté de l'Homoncule, abandonné avant l'achèvement. Il soigne encore, par réflexe.",
            "Rupture", "Alchimie", "Common", "Support", isElite: false,
            depthMin: 2, depthMax: 6, riskMin: 1, riskMax: 3,
            roomTypes: new[] { "Rupture", "Memory" },
            tags: new[] { "canon", "alchimie", "argile", "enfant" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.transmutation" },
            vitality: 16, attack: 4, defense: 6, guard: 2, speed: 9, focus: 3,
            cancellationToken);
    }

    // riskMin/riskMax are compared at runtime against ResolveCurrentEventCommandHandler's
    // catalogRiskLevel = Clamp(node.RiskLevel / 20 + 1, 1, 5) — a 1-5 bucket, NOT the raw
    // 0-100 node-risk scale. An enemy authored with riskMin >= 10 on that raw scale can
    // never be selected (MinRiskLevel <= 5 never holds), which silently excluded most of
    // the Bestiaire roster until this was caught. Always author riskMin/riskMax on 1-5.
    private async Task UpsertEnemyAsync(
        string key, string name, string description,
        string archetype, string family, string rank, string role, bool isElite,
        int depthMin, int depthMax, int riskMin, int riskMax,
        string[] roomTypes, string[] tags, string[] skillKeys,
        int vitality, int attack, int defense, int guard, int speed, int focus,
        CancellationToken cancellationToken = default,
        int magicAttack = 0, int magicDefense = 0, int initiative = 0, int mana = 0,
        int menace = 0, string rarity = "Common", string? registre = null,
        string[]? boundRoomKeys = null)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;

        var existing = await _ctx.EnemyDefinitions
            .Include(e => e.StatBlock)
            .Include(e => e.SkillLinks)
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);

        if (existing is null)
        {
            var enemy = new EnemyDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name,
                DisplayName = name,
                Description = description,
                NarrativeText = description,
                Version = version,
                Status = "Active",
                Archetype = archetype,
                Family = family,
                Rank = rank,
                Role = role,
                BaseDifficulty = isElite ? 2 : 1,
                EncounterWeight = 1,
                MinRiskLevel = riskMin,
                MaxRiskLevel = riskMax,
                MinDepth = depthMin,
                MaxDepth = depthMax,
                IsElite = isElite,
                BaseWeight = 1,
                Rarity = rarity,
                Registre = registre,
                MenaceLevel = menace,
                BoundRoomKeysJson = JsonSerializer.Serialize(boundRoomKeys ?? Array.Empty<string>()),
                CompatibleRoomTypesJson = JsonSerializer.Serialize(roomTypes),
                TagsJson = JsonSerializer.Serialize(tags),
                SkillKeysJson = JsonSerializer.Serialize(skillKeys),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            enemy.StatBlock = new EnemyStatBlockEntity
            {
                Id = Guid.NewGuid(),
                EnemyDefinitionId = enemy.Id,
                MaxVitality = vitality,
                AttackPower = attack,
                Defense = defense,
                StartingGuard = guard,
                Speed = speed,
                Initiative = initiative,
                Recovery = 0,
                Focus = focus,
                Mana = mana,
                Charge = 0,
                MagicAttack = magicAttack,
                MagicDefense = magicDefense
            };
            foreach (var skillKey in skillKeys.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                enemy.SkillLinks.Add(new EnemySkillLinkEntity
                {
                    EnemyDefinitionId = enemy.Id,
                    SkillDefinitionKey = skillKey
                });
            }
            _ctx.EnemyDefinitions.Add(enemy);
            return;
        }

        // Refresh metadata + stats (canon is authoritative for its own keys).
        existing.Name = name;
        existing.DisplayName = name;
        existing.Description = description;
        existing.NarrativeText = description;
        existing.Version = version;
        existing.Status = "Active";
        existing.Archetype = archetype;
        existing.Family = family;
        existing.Rank = rank;
        existing.Role = role;
        existing.BaseDifficulty = isElite ? 2 : 1;
        existing.MinRiskLevel = riskMin;
        existing.MaxRiskLevel = riskMax;
        existing.MinDepth = depthMin;
        existing.MaxDepth = depthMax;
        existing.IsElite = isElite;
        existing.Rarity = rarity;
        existing.Registre = registre;
        existing.MenaceLevel = menace;
        existing.BoundRoomKeysJson = JsonSerializer.Serialize(boundRoomKeys ?? Array.Empty<string>());
        existing.CompatibleRoomTypesJson = JsonSerializer.Serialize(roomTypes);
        existing.TagsJson = JsonSerializer.Serialize(tags);
        existing.SkillKeysJson = JsonSerializer.Serialize(skillKeys);
        existing.UpdatedAtUtc = now;

        existing.StatBlock ??= new EnemyStatBlockEntity
        {
            Id = Guid.NewGuid(),
            EnemyDefinitionId = existing.Id
        };
        existing.StatBlock.MaxVitality = vitality;
        existing.StatBlock.AttackPower = attack;
        existing.StatBlock.Defense = defense;
        existing.StatBlock.StartingGuard = guard;
        existing.StatBlock.Speed = speed;
        existing.StatBlock.Focus = focus;
        existing.StatBlock.Initiative = initiative;
        existing.StatBlock.Mana = mana;
        existing.StatBlock.MagicAttack = magicAttack;
        existing.StatBlock.MagicDefense = magicDefense;

        var desired = skillKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var skillKey in desired.Where(k => existing.SkillLinks.All(l => !string.Equals(l.SkillDefinitionKey, k, StringComparison.OrdinalIgnoreCase))))
        {
            existing.SkillLinks.Add(new EnemySkillLinkEntity
            {
                EnemyDefinitionId = existing.Id,
                SkillDefinitionKey = skillKey
            });
        }
    }

    // ── SORTS CANON ───────────────────────────────────────────────────────────
    private async Task SeedCanonSkillsAsync(CancellationToken cancellationToken)
    {
        // key, name, desc, skillType, targeting, effectType, mana, power
        // Referenced by every enemy/boss's skillKeys as their default attack — was
        // never actually seeded, causing "Missing skill definitions for keys:
        // skill.basic.strike" to throw as soon as any encounter tried to draft an enemy.
        await UpsertSkillAsync("skill.basic.strike", "Frappe",
            "Un coup simple, sans fioriture. Ce que tout ce qui a des poings ou des crocs sait faire.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 10, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.flamme-froide", "Flamme froide",
            "Bleu-violet, elle ne brûle pas la peau mais la chair, et le givre transperce l'os. Le sort de l'apothicaire.",
            "Damage", "SingleEnemy", "Damage", mana: 8, power: 22, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.priere-aspiration", "Prière",
            "Une prière lituique aspire la conscience. Elle restaure — mais nourrit ce qui rôde, et gonfle l'Égo.",
            "Drain", "SingleEnemy", "Debuff", mana: 4, power: 12, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -4, 3, Stat: "Defense") },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.transmutation", "Transmutation",
            "Plomb, or, mercure, soufre, sel. L'art alchimique réordonne la matière de l'instant.",
            "Buff", "Self", "Buff", mana: 6, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, 4, 3, Stat: "AttackPower") },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.brume", "Brume",
            "Le brouillard non-naturel se lève. Portée et précision s'effondrent — pour tous.",
            "Debuff", "AllEnemies", "Debuff", mana: 7, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -4, 3, Stat: "Focus") },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.flamme-seraphine", "Flamme Séraphine",
            "Le feu, le vrai. La seule terreur de l'Homoncule. Pure, dévorante, sans gel.",
            "Damage", "SingleEnemy", "Damage", mana: 12, power: 34, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.se-taire", "Se taire",
            "Ne rien dire. Ne pas prier. L'acte de silence. Inutile contre la chair — dévastateur contre ce qui se nourrit de la voix.",
            "Silence", "Self", "Status", mana: 0, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("Silence", null, 0, 3) },
            category: "Magic");

        // "Construction perpétuelle" (L'enfant, sort légendaire) : soin de 10% des PV max
        // et +8 de garde, tous deux répétés sur 5 tours (5 déclenchements, soit une
        // durée de 5 * TicksPerTurn).
        await UpsertSkillAsync("canon.skill.construction-perpetuelle", "Construction perpétuelle",
            "Ce que l'enfant a bâti continue de se construire, tour après tour, tant qu'on le laisse faire.",
            "Buff", "Self", "Buff", mana: 14, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("HealOverTime", null, 10, TicksPerTurn * 5,
                    TickInterval: TicksPerTurn, MagnitudeIsPercentOfMax: true),
                new SkillEffectSpec("GuardOverTime", null, 8, TicksPerTurn * 5,
                    TickInterval: TicksPerTurn)
            },
            category: "Magic");

        // "La liberté retrouvée" (Erina, sort légendaire) : frappe l'adversaire et
        // gagne +10% Vitesse (de base) pendant 10 tours ; l'effet est marqué
        // AppliesToActor car il doit revenir sur Erina/le lanceur, pas sur la cible
        // frappée.
        await UpsertSkillAsync("canon.skill.liberte-retrouvee", "La liberté retrouvée",
            "Un coup porté comme une évasion — et pour un temps, plus rien ne la retient.",
            "Damage", "SingleEnemy", "Damage", mana: 20, power: 14, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, 10, TicksPerTurn * 10,
                    Stat: "Speed", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true)
            },
            category: "Physical");

        // "Connaissance académique" (Pomenian, sort légendaire) : bénit toute l'équipe
        // de +10% dégâts des sorts (MagicDamageBonus) et -5% dégâts de sorts subis
        // (MagicDamageReduction). Cible AllAllies : chaque allié reçoit son propre
        // buff, donc pas d'AppliesToActor (la cible n'est déjà pas le lanceur seul).
        // StatusKey explicite sur chaque effet : deux StatModifier du même sort avec
        // une clé auto-générée identique (basée sur Kind seul) collisionneraient sinon
        // — le second Reinforce()rait le premier au lieu de créer son propre effet
        // (bug corrigé au passage, cf. "Une destinée cruelle").
        await UpsertSkillAsync("canon.skill.connaissance-academique", "Connaissance académique",
            "Un savoir cité comme on brandit une preuve — et pour un temps, l'équipe tout entière frappe et résiste comme s'il avait raison.",
            "Buff", "AllAllies", "Buff", mana: 22, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.connaissance-academique:magic-bonus", 10, TicksPerTurn * 5,
                    Stat: "MagicDamageBonus"),
                new SkillEffectSpec("StatModifier", "canon.skill.connaissance-academique:magic-reduction", 5, TicksPerTurn * 5,
                    Stat: "MagicDamageReduction")
            },
            category: "Magic");

        // "Regard infantile" (Iris, sort légendaire) : ralentit la cible de 10% Vitesse
        // (de base) pendant 5 tours — pas d'AppliesToActor, l'effet reste sur la cible.
        await UpsertSkillAsync("canon.skill.regard-infantile", "Regard infantile",
            "Un regard d'enfant, désarmant — de quoi faire hésiter n'importe qui, assez longtemps pour tout ralentir autour de lui.",
            "Debuff", "SingleEnemy", "Debuff", mana: 18, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, -10, TicksPerTurn * 5,
                    Stat: "Speed", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Magic");

        // "Frayeur organique" (Ethan) : une peur qui vient du corps, pas de la voix —
        // type Effroi intrinsèque au sort (voir EmotionalTypeProfileProvider.SkillTypesByKey),
        // indépendant de qui le lance.
        await UpsertSkillAsync("canon.skill.frayeur-organique", "Frayeur organique",
            "Une peur qui ne vient pas de la voix — elle sourd de lui, brute, organique, sans qu'il ait besoin de dire un mot.",
            "Damage", "SingleEnemy", "Damage", mana: 14, power: 16, cancellationToken,
            category: "Magic");

        // "Clairvoyance" (Araran) : réduit de 5 points la chance de coup critique de
        // tous les ennemis pendant 5 tours — réutilise le stat virtuel CriticalChanceBonus
        // (déjà introduit pour le Doudou de Ethan), ici en négatif et sur AllEnemies.
        await UpsertSkillAsync("canon.skill.clairvoyance", "Clairvoyance",
            "Elle voit venir le coup avant qu'il ne parte — assez pour désarmer sa précision, pour un temps.",
            "Debuff", "AllEnemies", "Debuff", mana: 16, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, -5, TicksPerTurn * 5,
                    Stat: "CriticalChanceBonus")
            },
            category: "Magic");

        // "Impulsivité" (Mané) : +5% vitesse (charge d'ATB), +5% dégâts (attaque), mais
        // -10% défense pendant 5 tours — auto-buff/débuff, donc AppliesToActor sur les
        // trois effets. StatusKey explicite sur chacun (sinon collision, cf.
        // "Connaissance académique" plus haut).
        await UpsertSkillAsync("canon.skill.impulsivite", "Impulsivité",
            "Agir avant de réfléchir — plus vite, plus fort, mais à découvert.",
            "Buff", "Self", "Buff", mana: 12, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.impulsivite:speed", 5, TicksPerTurn * 5,
                    Stat: "Speed", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true),
                new SkillEffectSpec("StatModifier", "canon.skill.impulsivite:attack", 5, TicksPerTurn * 5,
                    Stat: "AttackPower", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true),
                new SkillEffectSpec("StatModifier", "canon.skill.impulsivite:defense", -10, TicksPerTurn * 5,
                    Stat: "Defense", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true)
            },
            category: "Physical");

        // "Favorite de Elise" (Mané, sort légendaire) : +10% défense et +10% vitesse
        // (charge d'ATB) pendant 5 tours, et restaure instantanément 15% des PV max —
        // le seul sort canon à combiner un effet instantané (Heal, BasePower en % des
        // PV max) avec des buffs durables sur soi.
        // StatusKey explicite sur chaque effet (sinon collision, cf. "Connaissance académique").
        await UpsertSkillAsync("canon.skill.favorite-de-elise", "Favorite de Elise",
            "Elise veille sur ceux qu'elle préfère — une défense qui se referme, un pas plus vif, et ce qui a été perdu qui revient d'un coup.",
            "Buff", "Self", "Heal", mana: 24, power: 15, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.favorite-de-elise:defense", 10, TicksPerTurn * 5,
                    Stat: "Defense", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true),
                new SkillEffectSpec("StatModifier", "canon.skill.favorite-de-elise:speed", 10, TicksPerTurn * 5,
                    Stat: "Speed", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true)
            },
            category: "Magic",
            basePowerIsPercentOfMaxVitality: true);

        // "Création" (l'Architecte, sort légendaire) : le lanceur duplique temporairement
        // les sorts de sa cible pendant 10 tours. EffectType dédié ("CopySkills"),
        // résolu directement dans CombatSkillEffectResolver.ResolveCopySkills — power
        // encode ici le nombre de TOURS (et non une puissance), même degré de liberté
        // contextuelle par EffectType que Heal ("% des PV max")/Guard ailleurs.
        await UpsertSkillAsync("canon.skill.creation", "Création",
            "L'Architecte referme les yeux, imagine, et ce qu'il voit devient — pour un temps — vôtre aussi.",
            "Buff", "SingleEnemy", "CopySkills", mana: 20, power: 10, cancellationToken,
            category: "Magic");

        // "Écriture continuelle" (l'Écrivain, sort légendaire) : allonge de 25% le
        // nombre de ticks RESTANTS de tous les DamageOverTime actifs sur la cible.
        // EffectType dédié ("ExtendDotDuration"), résolu directement dans
        // CombatSkillEffectResolver.ResolveExtendDotDuration — power encode ici un
        // pourcentage (et non une puissance), même degré de liberté contextuelle par
        // EffectType que Heal ("% des PV max")/Création ("nb de tours").
        await UpsertSkillAsync("canon.skill.ecriture-continuelle", "Écriture continuelle",
            "Il n'arrête jamais d'écrire — et ce qu'il décrit continue, un peu plus longtemps que prévu, de se produire.",
            "Debuff", "SingleEnemy", "ExtendDotDuration", mana: 16, power: 25, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.larme-des-enfers", "Larme des enfers",
            "En apprenant à invoquer le fleuve des enfers, quelques gouttes suffisent à provoquer une maladie infecte qui ronge les chairs.",
            "Damage", "SingleEnemy", "Damage", mana: 14, power: 10, cancellationToken,
            effects: new[] { new SkillEffectSpec("DamageOverTime", null, 7, TicksPerTurn * 15, TickInterval: TicksPerTurn) },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.souffle-de-la-forge", "Souffle de la forge",
            "Cette puissante machine crache ses flammes jusqu'au tréfonds des enfers. Celui qui sait la manier peut calciner son adversaire jusqu'à ce que ses os fondent.",
            "Damage", "SingleEnemy", "Damage", mana: 16, power: 10, cancellationToken,
            effects: new[] { new SkillEffectSpec("DamageOverTime", null, 10, TicksPerTurn * 10, TickInterval: TicksPerTurn) },
            category: "Physical");

        // "Contemplation infinie" : "3 charges complètes" = 3 tours par notre convention
        // partagée (voir AtbConstants.TicksPerTurn côté moteur).
        await UpsertSkillAsync("canon.skill.contemplation-infinie", "Contemplation infinie",
            "En se perdant dans les méandres du Palais, la vérité apparaît, mais la clairvoyance pousse à l'immobilité.",
            "Debuff", "SingleEnemy", "Debuff", mana: 18, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, -25, TicksPerTurn * 3,
                    Stat: "Speed", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Magic");

        // "Silence" : bloque complètement la prochaine action de la cible (voir
        // Combatant.IsAtbLocked / Combat.cs — désormais câblé, contrairement à l'ancien
        // "Se taire" dont le Silence n'était encore branché nulle part).
        await UpsertSkillAsync("canon.skill.silence", "Silence",
            "Le silence n'est pas seulement une manière de réfléchir, mais il est également une punition à ceux qui se montrent trop agressifs.",
            "Debuff", "SingleEnemy", "Debuff", mana: 14, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("Silence", null, 0, TicksPerTurn) },
            category: "Magic");

        // Type émotionnel intrinsèque (Mémoire) déclaré dans EmotionalTypeProfileProvider.SkillTypesByKey.
        await UpsertSkillAsync("canon.skill.sursaut-memoriel", "Sursaut mémoriel",
            "La mémoire est une réalité qu'il faut trop souvent fuir. Mais rappelez-vous, et souffrez pour accepter.",
            "Damage", "SingleEnemy", "Damage", mana: 20, power: 12, cancellationToken,
            effects: new[] { new SkillEffectSpec("DamageOverTime", null, 11, TicksPerTurn * 15, TickInterval: TicksPerTurn) },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.larme-elise", "Larme d'Elise",
            "Le silence et l'apathie d'Elise ne sont que des façades. Au fond d'elle brille un espoir et un amour aussi grand que le Palais.",
            "Buff", "Self", "Heal", mana: 18, power: 5, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("HealOverTime", null, 2, TicksPerTurn * 10,
                    TickInterval: TicksPerTurn, MagnitudeIsPercentOfMax: true, AppliesToActor: true)
            },
            category: "Magic",
            basePowerIsPercentOfMaxVitality: true);

        await UpsertSkillAsync("canon.skill.caresse-de-mane", "Caresse de Mané",
            "La pureté et la gentillesse de Mané dans un seul geste.",
            "Buff", "Self", "Heal", mana: 16, power: 15, cancellationToken,
            category: "Magic",
            basePowerIsPercentOfMaxVitality: true);

        // Type émotionnel intrinsèque (Déni) déclaré dans EmotionalTypeProfileProvider.SkillTypesByKey.
        await UpsertSkillAsync("canon.skill.anagramme", "Anagramme",
            "Inverse les lettres, change les mots, change de personne et attaque sous une nouvelle identité.",
            "Damage", "SingleEnemy", "Damage", mana: 14, power: 17, cancellationToken,
            category: "Magic");

        // Type émotionnel intrinsèque (Silence) déclaré dans EmotionalTypeProfileProvider.SkillTypesByKey.
        await UpsertSkillAsync("canon.skill.lecture-des-silences", "Lecture des silences",
            "Lire des passages du tome de silence n'est pas donné à tous, mais ceux qui y arrivent font peser le silence sur les ennemis.",
            "Damage", "SingleEnemy", "Damage", mana: 14, power: 15, cancellationToken,
            category: "Magic");

        // Type émotionnel intrinsèque (Effroi) déclaré dans EmotionalTypeProfileProvider.SkillTypesByKey.
        await UpsertSkillAsync("canon.skill.nevrose", "Névrose",
            "Plonger son ennemi dans une névrose profonde, lui dictant des passages du tome des silences.",
            "Damage", "SingleEnemy", "Damage", mana: 16, power: 10, cancellationToken,
            effects: new[] { new SkillEffectSpec("DamageOverTime", null, 6, TicksPerTurn * 10, TickInterval: TicksPerTurn) },
            category: "Magic");

        // Type émotionnel intrinsèque (Folie, nouveau registre) déclaré dans
        // EmotionalTypeProfileProvider.SkillTypesByKey. Malédiction à double tranchant :
        // la cible devient plus forte (Attaque/Vitesse/Focus +7%) tout en se consumant
        // (DoT), les deux durant la même fenêtre de 15 tours.
        await UpsertSkillAsync("canon.skill.plongee-dans-la-folie", "Plongée dans la folie",
            "Fait perdre tout contact avec la réalité à la cible, la rendant extrêmement puissante mais la faisant se consumer à petit feu.",
            "Damage", "SingleEnemy", "Damage", mana: 24, power: 20, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("DamageOverTime", null, 10, TicksPerTurn * 15, TickInterval: TicksPerTurn),
                new SkillEffectSpec("StatModifier", "canon.skill.plongee-dans-la-folie:attack", 7, TicksPerTurn * 15, Stat: "AttackPower", MagnitudeIsPercentOfBaseStat: true),
                new SkillEffectSpec("StatModifier", "canon.skill.plongee-dans-la-folie:speed", 7, TicksPerTurn * 15, Stat: "Speed", MagnitudeIsPercentOfBaseStat: true),
                new SkillEffectSpec("StatModifier", "canon.skill.plongee-dans-la-folie:focus", 7, TicksPerTurn * 15, Stat: "Focus", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.egide", "Égide",
            "Lève une égide mentale pour se protéger des assaillants actuels.",
            "Buff", "Self", "Guard", mana: 10, power: 15, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.rempart", "Rempart",
            "Élève de puissants remparts pour contrecarrer les attaques ennemies.",
            "Buff", "AllAllies", "Guard", mana: 16, power: 7, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.symphonie-des-enfers", "Symphonie des enfers",
            "Les enfers vous viennent en aide ; ils veulent récupérer toutes les âmes qui se trouvent face à vous.",
            "Damage", "AllEnemies", "Damage", mana: 26, power: 6, cancellationToken,
            effects: new[] { new SkillEffectSpec("DamageOverTime", null, 4, TicksPerTurn * 15, TickInterval: TicksPerTurn) },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.deluge-du-styx", "Déluge du Styx",
            "Ouvre une brèche pour laisser se déverser les eaux des enfers, empoisonnant les ennemis.",
            "Debuff", "AllEnemies", "Debuff", mana: 22, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("DamageOverTime", null, 6, TicksPerTurn * 15, TickInterval: TicksPerTurn) },
            category: "Magic");

        // "Une destinée cruelle" : transformation permanente (jusqu'à la mort) — le
        // seul sort canon à utiliser IsPermanent: true. +20% à Attaque/Défense/Vitesse/
        // Focus, ET séparément -15% sur la vitesse de remplissage de jauge ATB elle-même
        // (AtbTempoModifier, indépendant du stat Vitesse — voir Combatant.
        // RecalculateAtbFillPerTick), ET un DoT de 10% des PV max par tour, sans fin.
        await UpsertSkillAsync("canon.skill.destinee-cruelle", "Une destinée cruelle",
            "Il faut parfois savoir chercher au plus profond de soi pour repousser ses limites, quel qu'en soit le prix.",
            "Buff", "Self", "Buff", mana: 30, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.destinee-cruelle:attack", 20, 0, Stat: "AttackPower", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true, IsPermanent: true),
                new SkillEffectSpec("StatModifier", "canon.skill.destinee-cruelle:defense", 20, 0, Stat: "Defense", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true, IsPermanent: true),
                new SkillEffectSpec("StatModifier", "canon.skill.destinee-cruelle:speed", 20, 0, Stat: "Speed", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true, IsPermanent: true),
                new SkillEffectSpec("StatModifier", "canon.skill.destinee-cruelle:focus", 20, 0, Stat: "Focus", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true, IsPermanent: true),
                new SkillEffectSpec("StatModifier", "canon.skill.destinee-cruelle:tempo", -15, 0, Stat: "AtbTempoModifier", AppliesToActor: true, IsPermanent: true),
                new SkillEffectSpec("DamageOverTime", "canon.skill.destinee-cruelle:dot", 10, 0, TickInterval: TicksPerTurn, MagnitudeIsPercentOfMax: true, AppliesToActor: true, IsPermanent: true)
            },
            category: "Magic");

        // ── Sorts de kit compagnon (chantier "Compagnons") ─────────────────────

        // "Fondations" (Thomas, kit compagnon) : garde instantanée + défense renforcée
        // sur une durée — thème tank/support structurel, cohérent avec sa persona
        // (l'Architecte). Cible SingleAlly : les effets s'appliquent à la cible choisie,
        // pas au lanceur (pas de AppliesToActor, cf. "Rempart").
        await UpsertSkillAsync("canon.skill.fondations-de-thomas", "Fondations",
            "Il pose, sous les pas d'un allié, quelque chose d'aussi stable que le Palais lui-même.",
            "Buff", "SingleAlly", "Guard", mana: 14, power: 15, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.fondations-de-thomas:defense", 10, TicksPerTurn * 4,
                    Stat: "Defense", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Physical");

        // "Veillée" (Mina, kit compagnon) : garde qui se régénère sur quelques tours —
        // thème protecteur/enfantin, cohérent avec sa persona (veillée par Him'Lit).
        await UpsertSkillAsync("canon.skill.veillee-de-mina", "Veillée",
            "Elle veille sur toi comme Him'Lit veille sur elle — une garde qui revient, tour après tour.",
            "Buff", "SingleAlly", "Buff", mana: 18, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("GuardOverTime", null, 6, TicksPerTurn * 5, TickInterval: TicksPerTurn)
            },
            category: "Magic");

        // "Baiser d'Elise" (Elise, butin rare) : soin instantané de 20% des PV max —
        // le seul geste concret d'Elise, elle qui ne répond jamais aux questions.
        await UpsertSkillAsync("canon.skill.baiser-delise", "Baiser d'Elise",
            "Elle ne dit rien. Elle pose seulement les lèvres, et ce qui était perdu revient, un peu.",
            "Heal", "SingleAlly", "Heal", mana: 16, power: 20, cancellationToken,
            category: "Magic",
            basePowerIsPercentOfMaxVitality: true);

        // "Vol à la tire" (John, kit compagnon) : frappe rapide et gain de précision
        // critique pour le lanceur — thème rapide/opportuniste, cohérent avec sa
        // persona (ancien voleur). AppliesToActor: true car le bonus doit revenir sur
        // John, pas sur la cible frappée (cf. "La liberté retrouvée").
        await UpsertSkillAsync("canon.skill.vol-a-la-tire", "Vol à la tire",
            "Un geste vif, appris dans les ruines, avant que le Palais ne lui envoie Him'Lit.",
            "Damage", "SingleEnemy", "Damage", mana: 10, power: 16, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.vol-a-la-tire:crit", 10, TicksPerTurn,
                    Stat: "CriticalChanceBonus", AppliesToActor: true)
            },
            category: "Physical");
    }

    // ── BESTIAIRE — LES VEILLEURS DU SEUIL (Silence · Hall d'entrée, Couloirs, Palier) ──
    // Première famille du Bestiaire officiel du Palais : silhouettes de service nées
    // des offenses faites au tapis/seuil du Hall, prolongeant le rituel d'hospitalité
    // du Majordome. Mécanique de famille "Le Protocole" (tant que le Porteur de
    // Plateau est en vie, les debuffs des Veilleurs durent plus longtemps ; le tuer
    // brise le Protocole) et les 5 réactions "Attitude en combat" par créature sont
    // documentées ici en NarrativeText mais ne sont PAS câblées mécaniquement — elles
    // demandent un nouveau hook moteur (réaction au dégât subi / mort d'un allié) qui
    // n'existe pas encore ; c'est le prochain étage du chantier, pas cette passe.
    private async Task SeedBestiaireVeilleursDuSeuilAsync(CancellationToken cancellationToken)
    {
        const string family = "Veilleurs du Seuil";
        const string registre = "Silence";

        // Pli du tapis (Veilleur du Tapis) : frappe basique flavorée, le tapis claque comme un fouet.
        await UpsertSkillAsync("canon.skill.pli-du-tapis", "Pli du tapis",
            "Le tapis se soulève et claque comme un fouet.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 9, cancellationToken,
            category: "Physical");

        // Étouffement feutré (Veilleur du Tapis) : -15% Vitesse, 3 tours.
        await UpsertSkillAsync("canon.skill.etouffement-feutre", "Étouffement feutré",
            "Enroule la cible : le tapis absorbe le bruit des pas.",
            "Debuff", "SingleEnemy", "Debuff", mana: 8, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, -15, TicksPerTurn * 3,
                    Stat: "Speed", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Magic");

        // Seuil souillé (Veilleur du Tapis) : punition du protocole — l'IA du Veilleur
        // ne la lance que sur un adversaire ayant attaqué un Veilleur allié ce tour
        // (règle comportementale, non une restriction de ciblage moteur).
        await UpsertSkillAsync("canon.skill.seuil-souille", "Seuil souillé",
            "Punition du protocole : quiconque frappe un Veilleur en répond devant tous les autres.",
            "Damage", "SingleEnemy", "Debuff", mana: 12, power: 14, cancellationToken,
            category: "Magic");

        // Service du thé (Porteur de Plateau) : soin 12% PV max.
        await UpsertSkillAsync("canon.skill.service-du-the", "Service du thé",
            "Soigne l'allié le plus blessé. La première tasse fume toujours.",
            "Heal", "SingleAlly", "Heal", mana: 10, power: 12, cancellationToken,
            category: "Magic", basePowerIsPercentOfMaxVitality: true);

        // Tasse retournée (Porteur de Plateau) : poison 5 dégâts/tour, 6 tours.
        await UpsertSkillAsync("canon.skill.tasse-retournee", "Tasse retournée",
            "La troisième tasse trouve enfin preneur.",
            "Debuff", "SingleEnemy", "Debuff", mana: 12, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("DamageOverTime", null, 5, TicksPerTurn * 6, TickInterval: TicksPerTurn)
            },
            category: "Magic");

        // Étiquette (Porteur de Plateau) : +3 Focus brut à toute l'équipe, 3 tours.
        await UpsertSkillAsync("canon.skill.etiquette", "Étiquette",
            "Le service se resserre.",
            "Buff", "AllAllies", "Buff", mana: 8, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, 3, TicksPerTurn * 3, Stat: "Focus") },
            category: "Magic");

        // Formule creuse (Écho de Politesse) : dégâts magiques.
        await UpsertSkillAsync("canon.skill.formule-creuse", "Formule creuse",
            "Les mots vides pèsent plus lourd qu'on ne croit.",
            "Damage", "SingleEnemy", "Damage", mana: 9, power: 13, cancellationToken,
            category: "Magic");

        // Courbette inversée (Écho de Politesse) : la doc décrit un renvoi de 30% des
        // prochains dégâts magiques subis — approximé ici en réduction de dégâts
        // magiques (pas de mécanique de renvoi au moteur ; réduction défensive plutôt
        // que réflexion offensive, simplification à assumer/affiner plus tard).
        await UpsertSkillAsync("canon.skill.courbette-inversee", "Courbette inversée",
            "La politesse se retourne.",
            "Buff", "Self", "Buff", mana: 11, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, 30, TicksPerTurn * 2, Stat: "MagicDamageReduction")
            },
            category: "Magic");

        // Chute de marbre (Sentinelle du Seuil) : frappe lourde. La doc décrit -1 tour
        // d'ATB pour la Sentinelle elle-même (coût d'auto-interruption) — non câblé
        // (aucun effet "auto-interruption" authorable aujourd'hui), simplification à
        // assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.chute-de-marbre", "Chute de marbre",
            "Frappe lourde mono-cible. Elle doit se redresser.",
            "Damage", "SingleEnemy", "Damage", mana: 6, power: 18, cancellationToken,
            category: "Physical");

        // Socle (Sentinelle du Seuil) : garde instantanée de 15.
        await UpsertSkillAsync("canon.skill.socle", "Socle",
            "Redevient pilier un instant.",
            "Buff", "Self", "Guard", mana: 10, power: 15, cancellationToken,
            category: "Physical");

        // Verdict du seuil (Sentinelle du Seuil) : exécution. La doc exige -DEF ET
        // -Focus actifs sur la cible — règle comportementale (IA), non une
        // restriction de ciblage moteur.
        await UpsertSkillAsync("canon.skill.verdict-du-seuil", "Verdict du seuil",
            "Le protocole est complet ; la sentence tombe.",
            "Damage", "SingleEnemy", "Damage", mana: 16, power: 28, cancellationToken,
            category: "Magic");

        // roomTypes uses "Memory"/"Silence", not "Threshold": no Palais room theme ever
        // parses to RoomType.Threshold (DeterministicRunGenerator.MapThemeToScaffold falls
        // back to Memory for unrecognized themes), and room.halldentree/room.palier both
        // resolve to Memory while room.couloirs resolves to Silence — matching this family's
        // BoundRoomKeys exactly so the coarse RoomType filter never excludes them.
        await UpsertEnemyAsync(
            "canon.enemy.veilleur-tapis", "Veilleur du Tapis",
            "Une silhouette de majordome sans visage, penchée en permanence vers le sol, comme figée dans une révérence qui n'a jamais eu le droit de se relever. Ses mains gantées lissent inlassablement un pan de tapis bordeaux qui le suit où qu'il aille, cousu à ses chevilles. « Vos pieds. Je vous prie. »",
            "Guard", family, "Common", "Guard", isElite: false,
            depthMin: 1, depthMax: 3, riskMin: 1, riskMax: 3,
            roomTypes: new[] { "Memory", "Silence" },
            tags: new[] { "bestiaire", "silence", "veilleurs-du-seuil", "protocole" },
            skillKeys: new[] { "canon.skill.pli-du-tapis", "canon.skill.rempart", "canon.skill.etouffement-feutre", "canon.skill.seuil-souille" },
            vitality: 62, attack: 7, defense: 10, guard: 0, speed: 6, focus: 3,
            cancellationToken,
            magicAttack: 4, magicDefense: 8, initiative: 5, mana: 10, menace: 2,
            rarity: "Common", registre: registre,
            boundRoomKeys: new[] { "room.halldentree", "room.couloirs" });

        await UpsertEnemyAsync(
            "canon.enemy.porteur-plateau", "Porteur de Plateau",
            "Un torse en livrée, sans jambes, flottant à hauteur exacte de service. Sur son plateau d'argent : trois tasses. La première fume, la deuxième est vide, la troisième est retournée. Personne n'a jamais bu la troisième. « Thé ? Eau ? Attention ? »",
            "Support", family, "Common", "Support", isElite: false,
            depthMin: 1, depthMax: 4, riskMin: 1, riskMax: 3,
            roomTypes: new[] { "Memory", "Silence" },
            tags: new[] { "bestiaire", "silence", "veilleurs-du-seuil", "protocole" },
            skillKeys: new[] { "canon.skill.priere-aspiration", "canon.skill.service-du-the", "canon.skill.tasse-retournee", "canon.skill.etiquette" },
            vitality: 44, attack: 4, defense: 5, guard: 0, speed: 9, focus: 6,
            cancellationToken,
            magicAttack: 9, magicDefense: 9, initiative: 9, mana: 18, menace: 4,
            rarity: "Common", registre: registre,
            boundRoomKeys: new[] { "room.halldentree", "room.couloirs", "room.palier" });

        await UpsertEnemyAsync(
            "canon.enemy.echo-politesse", "Écho de Politesse",
            "Une brume en forme de courbette. On la distingue à peine dans les couloirs distordus : un pli dans l'air qui s'incline sur votre passage et ne se redresse que dans votre dos. « Après vous. Non — après vous. »",
            "Disruptor", family, "Common", "Disruptor", isElite: false,
            depthMin: 1, depthMax: 5, riskMin: 1, riskMax: 3,
            roomTypes: new[] { "Memory", "Silence" },
            tags: new[] { "bestiaire", "silence", "veilleurs-du-seuil" },
            skillKeys: new[] { "canon.skill.brume", "canon.skill.formule-creuse", "canon.skill.courbette-inversee", "canon.skill.se-taire" },
            vitality: 38, attack: 3, defense: 4, guard: 0, speed: 11, focus: 7,
            cancellationToken,
            magicAttack: 10, magicDefense: 11, initiative: 11, mana: 16, menace: 3,
            rarity: "Uncommon", registre: registre,
            boundRoomKeys: new[] { "room.couloirs", "room.palier" });

        await UpsertEnemyAsync(
            "canon.enemy.sentinelle-seuil", "Sentinelle du Seuil",
            "Un pilier de marbre du Hall — l'un des quatre — descendu de son socle. Des veines bleu-violet parcourent sa pierre : la Flamme froide dort dedans. Il marche lentement, et le sol s'essuie tout seul devant ses pas. « Le seuil a été souillé. Cela ne se pardonne pas. »",
            "Bruiser", family, "Elite", "Bruiser", isElite: true,
            depthMin: 2, depthMax: 6, riskMin: 1, riskMax: 4,
            roomTypes: new[] { "Memory", "Silence" },
            tags: new[] { "bestiaire", "silence", "veilleurs-du-seuil", "elite" },
            skillKeys: new[] { "canon.skill.flamme-froide", "canon.skill.chute-de-marbre", "canon.skill.socle", "canon.skill.verdict-du-seuil" },
            vitality: 88, attack: 11, defense: 12, guard: 0, speed: 5, focus: 4,
            cancellationToken,
            magicAttack: 12, magicDefense: 7, initiative: 4, mana: 20, menace: 6,
            rarity: "Rare", registre: registre,
            boundRoomKeys: new[] { "room.halldentree", "room.couloirs" });
    }

    private async Task SeedBestiaireCopistesAsync(CancellationToken cancellationToken)
    {
        const string family = "Copistes";
        const string registre = "Memoire";

        // Dictée (Copiste Aveugle) : marque la cible d'une petite entaille magique —
        // la clé de statut dérivée ("canon.skill.dictee:StatModifier") sert aussi de
        // repère à l'IA pour retrouver "la cible marquée" au tour suivant (voir
        // CopisteAveugleBossBehavior). Le vrai bonus documenté ("+2 dégâts/tour au
        // prochain DoT subi") n'est pas câblé — aucun hook de magnitude conditionnelle
        // sur un DoT à venir n'existe côté moteur ; approximé par une vulnérabilité
        // magique mineure et immédiate.
        await UpsertSkillAsync("canon.skill.dictee", "Dictée",
            "Marque la cible : ce qui est dicté est aggravé.",
            "Debuff", "SingleEnemy", "Debuff", mana: 10, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, -4, TicksPerTurn * 2, Stat: "MagicDefense")
            },
            category: "Magic");

        // Plume sèche (Copiste Aveugle) : frappe de repli quand le mana manque.
        await UpsertSkillAsync("canon.skill.plume-seche", "Plume sèche",
            "Griffure de plume. Frappe de repli quand le mana manque.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 8, cancellationToken,
            category: "Physical");

        // Encre vive (Encrier Vivant) : DoT pur, l'encre pénètre et continue d'écrire.
        await UpsertSkillAsync("canon.skill.encre-vive", "Encre vive",
            "L'encre pénètre et continue d'écrire sous la peau.",
            "Debuff", "SingleEnemy", "Debuff", mana: 10, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("DamageOverTime", null, 6, TicksPerTurn * 8, TickInterval: TicksPerTurn)
            },
            category: "Magic");

        // Recharge (Encrier Vivant) : transfère 8 mana à un allié — EffectType
        // "RestoreMana", nouveau au moteur (CombatSkillEffectResolver.ResolveRestoreMana),
        // ajouté avec cette famille pour porter le rôle de réservoir tactique du groupe.
        await UpsertSkillAsync("canon.skill.recharge", "Recharge",
            "L'encrier se penche, l'allié trempe sa plume.",
            "Buff", "SingleAlly", "RestoreMana", mana: 0, power: 8, cancellationToken,
            category: "Magic");

        // Éclaboussure (Encrier Vivant) : dégâts de zone légers + -2 Focus (2 tours).
        await UpsertSkillAsync("canon.skill.eclaboussure", "Éclaboussure",
            "L'encre gicle dans les yeux.",
            "Damage", "AllEnemies", "Damage", mana: 12, power: 10, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -2, TicksPerTurn * 2, Stat: "Focus") },
            category: "Magic");

        // Corps de verre (Encrier Vivant) : garde instantanée de 10.
        await UpsertSkillAsync("canon.skill.corps-de-verre", "Corps de verre",
            "Durcit sa paroi fêlée.",
            "Buff", "Self", "Guard", mana: 8, power: 10, cancellationToken,
            category: "Physical");

        // Phrase inachevée (Page Inachevée) : dégâts magiques. Le bonus documenté
        // ("+50% si la cible canalisait ou vient de subir Silence") n'est pas câblé
        // comme multiplicateur — l'IA compense en priorisant ce sort sur une cible
        // déjà sous Silence (voir PageInacheveeBossBehavior), donc le synergisme
        // narratif reste respecté même sans bonus de puissance conditionnel.
        await UpsertSkillAsync("canon.skill.phrase-inachevee", "Phrase inachevée",
            "Dégâts. Si la cible était en train de canaliser ou vient de subir Silence : la phrase frappe plus fort.",
            "Damage", "SingleEnemy", "Damage", mana: 10, power: 12, cancellationToken,
            category: "Magic");

        // Marge blanche (Page Inachevée) : la doc décrit l'effacement du plus récent
        // buff de la cible — aucun mécanisme de dissipation ciblée n'existe côté
        // moteur ; approximé par un débuff de Focus (« ce qui n'est pas écrit
        // n'existe pas »), simplification à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.marge-blanche", "Marge blanche",
            "Ce qui n'est pas écrit n'existe pas.",
            "Debuff", "SingleEnemy", "Debuff", mana: 8, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -3, TicksPerTurn * 2, Stat: "Focus") },
            category: "Magic");

        // Repli de papier (Page Inachevée) : la doc décrit une esquive garantie de la
        // prochaine attaque mono-cible (1x/4 tours) — aucun statut d'esquive garantie
        // n'existe côté moteur ; approximé par une garde instantanée légère,
        // simplification à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.repli-de-papier", "Repli de papier",
            "Se plie sur elle-même.",
            "Buff", "Self", "Guard", mana: 0, power: 8, cancellationToken,
            category: "Physical");

        // Couture (Le Relieur) : l'aiguille traverse. La doc conditionne -3 Vitesse à
        // "2+ DoT actifs sur la cible" — appliqué ici systématiquement (pas de lecture
        // conditionnelle du nombre de DoT adverses dans l'auteurisation d'un sort),
        // simplification mineure à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.couture", "Couture",
            "L'aiguille traverse. Cousue sur place.",
            "Damage", "SingleEnemy", "Damage", mana: 8, power: 16, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -3, TicksPerTurn * 3, Stat: "Speed") },
            category: "Physical");

        // Reliure de chair (Le Relieur) : la doc décrit un partage de 30% des dégâts
        // subis entre deux adversaires liés — aucune mécanique de partage de dégâts
        // entre deux cibles n'existe côté moteur ; approximé par un débuff de
        // Défense sur la cible désignée (« liée, donc affaiblie »), simplification à
        // assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.reliure-de-chair", "Reliure de chair",
            "Lie deux adversaires : la douleur de l'un rejaillit sur l'autre.",
            "Debuff", "SingleEnemy", "Debuff", mana: 14, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, -15, TicksPerTurn * 3, Stat: "Defense", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Magic");

        // Nœud final (Le Relieur) : exécution. La doc ajoute +8 dégâts par DoT actif
        // sur la cible — aucune lecture du nombre de DoT adverses dans le calcul de
        // puissance d'un sort n'existe côté moteur ; puissance fixe ici,
        // simplification à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.noeud-final", "Nœud final",
            "Le livre se ferme.",
            "Damage", "SingleEnemy", "Damage", mana: 20, power: 24, cancellationToken,
            category: "Magic");

        await UpsertEnemyAsync(
            "canon.enemy.copiste-aveugle", "Copiste Aveugle",
            "Un scribe voûté dont les orbites sont scellées de cire à cacheter. Ses doigts, terminés par des plumes, courent sur un parchemin déroulé à même l'air. Il recopie tout ce qui se passe dans la pièce — les gestes, les cris, les silences — en temps réel. « Je n'ai pas besoin de voir. Le texte se souvient pour moi. »",
            "Disruptor", family, "Common", "Disruptor", isElite: false,
            depthMin: 2, depthMax: 6, riskMin: 1, riskMax: 3,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "memoire", "copistes", "dot" },
            skillKeys: new[] { "canon.skill.dictee", "canon.skill.sursaut-memoriel", "canon.skill.lecture-des-silences", "canon.skill.plume-seche" },
            vitality: 46, attack: 4, defense: 5, guard: 0, speed: 8, focus: 8,
            cancellationToken,
            magicAttack: 11, magicDefense: 9, initiative: 8, mana: 20, menace: 3,
            rarity: "Common", registre: registre,
            boundRoomKeys: new[] { "room.palier", "room.labyrinthe" });

        await UpsertEnemyAsync(
            "canon.enemy.encrier-vivant", "Encrier Vivant",
            "Une masse d'encre noire contenue dans un corps de verre fêlé, à peu près humanoïde. Elle laisse derrière elle des flaques qui forment des mots — toujours les mêmes : les premières pièces du Palais, décrites à l'infini. « Il ne faut jamais, jamais manquer d'encre. »",
            "Support", family, "Common", "Support", isElite: false,
            depthMin: 2, depthMax: 7, riskMin: 1, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "memoire", "copistes" },
            skillKeys: new[] { "canon.skill.recharge", "canon.skill.encre-vive", "canon.skill.eclaboussure", "canon.skill.corps-de-verre" },
            vitality: 58, attack: 6, defense: 8, guard: 0, speed: 6, focus: 5,
            cancellationToken,
            magicAttack: 8, magicDefense: 10, initiative: 5, mana: 26, menace: 3,
            rarity: "Common", registre: registre,
            boundRoomKeys: new[] { "room.labyrinthe", "room.enfer3" });

        await UpsertEnemyAsync(
            "canon.enemy.page-inachevee", "Page Inachevée",
            "Une feuille immense, déchirée à mi-hauteur, qui flotte verticalement. Le texte qu'elle porte s'interrompt en plein mot. Ceux qui la lisent trop longtemps sentent leur propre pensée s'interrompre au même endroit, encore et encore. « La phrase s'arrête ici. Vous aussi. »",
            "Disruptor", family, "Uncommon", "Disruptor", isElite: false,
            depthMin: 2, depthMax: 7, riskMin: 1, riskMax: 4,
            roomTypes: new[] { "Silence", "Memory" },
            tags: new[] { "bestiaire", "silence", "copistes", "control" },
            skillKeys: new[] { "canon.skill.silence", "canon.skill.phrase-inachevee", "canon.skill.marge-blanche", "canon.skill.repli-de-papier" },
            vitality: 36, attack: 3, defense: 3, guard: 0, speed: 12, focus: 9,
            cancellationToken,
            magicAttack: 10, magicDefense: 12, initiative: 12, mana: 22, menace: 4,
            rarity: "Uncommon", registre: "Silence",
            boundRoomKeys: new[] { "room.palier", "room.labyrinthe" });

        await UpsertEnemyAsync(
            "canon.enemy.relieur", "Le Relieur",
            "Un artisan massif au tablier de cuir, dont les bras se terminent en aiguilles courbes enfilées de nerf. Il ne relie pas des livres : il relie des instants entre eux, cousant la douleur d'hier à celle de demain pour qu'aucune ne puisse finir. « Rien ne se termine tant que je n'ai pas cousu la dernière page. »",
            "Bruiser", family, "Rare", "Bruiser", isElite: true,
            depthMin: 3, depthMax: 8, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "memoire", "copistes", "elite", "dot" },
            skillKeys: new[] { "canon.skill.ecriture-continuelle", "canon.skill.couture", "canon.skill.reliure-de-chair", "canon.skill.noeud-final" },
            vitality: 92, attack: 10, defense: 9, guard: 0, speed: 5, focus: 7,
            cancellationToken,
            magicAttack: 13, magicDefense: 10, initiative: 4, mana: 24, menace: 7,
            rarity: "Rare", registre: registre,
            boundRoomKeys: new[] { "room.labyrinthe" });
    }

    private async Task SeedBestiaireSqueletteDeSouvenirsAsync(CancellationToken cancellationToken)
    {
        const string family = "Squelettes de Souvenirs";
        const string registre = "Memoire";

        // Mécanique de famille "L'Ossuaire" (un Squelette mort laisse un Ossement au
        // sol ; le Porteur de Cendre peut le consommer pour relever ce Squelette à
        // 40% PV) et la remise "-2 mana Silence dans la Calamité" ne sont pas
        // modélisées — nécessiteraient respectivement un hook "on ally death" avec
        // suivi d'un jeton persistant côté combat, et une conscience de la salle dans
        // le calcul du coût d'un sort. Différé, comme la Rature des Veilleurs du
        // Seuil et l'Attitude en combat en général. "Effondrement" et "Braise
        // mémorielle" ci-dessous sont donc des approximations : la première perd son
        // volet "meurt et relance l'Ossuaire" (ne reste que les dégâts de zone), la
        // seconde perd sa fonction de relance et devient une garde défensive de
        // repli — jamais choisie par l'IA (voir PorteurCendreBossBehavior).

        await UpsertSkillAsync("canon.skill.griffe-dos", "Griffe d'os",
            "Frappe simple. Ce que tout ce qui a des phalanges sait faire.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 10, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.fragment-grave", "Fragment gravé",
            "Lance un éclat d'os gravé : la cible voit le souvenir.",
            "Damage", "SingleEnemy", "Damage", mana: 6, power: 8, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -2, TicksPerTurn * 2, Stat: "Focus") },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.etreinte-creuse", "Étreinte creuse",
            "Agrippe : il cherche quelqu'un pour se souvenir de lui.",
            "Damage", "SingleEnemy", "Damage", mana: 4, power: 6, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, -10, TicksPerTurn * 2, Stat: "Speed", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Physical");

        await UpsertSkillAsync("canon.skill.effondrement", "Effondrement",
            "Se démembre volontairement : dégâts de zone.",
            "Damage", "AllEnemies", "Damage", mana: 0, power: 6, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.braise-memorielle", "Braise mémorielle",
            "Rallume un Ossement — pour l'instant, ne fait que se replier prudemment.",
            "Buff", "Self", "Guard", mana: 8, power: 6, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.jet-de-cendre", "Jet de cendre",
            "La cendre entre dans les yeux et la mémoire.",
            "Damage", "SingleEnemy", "Damage", mana: 7, power: 9, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -3, TicksPerTurn * 3, Stat: "Focus") },
            category: "Magic");

        // "Fardeau partagé" (Porteur de Cendre) : la doc décrit un coût de -8% PV max
        // pour le Porteur en échange du soin — non câblé (un sort n'a qu'une seule
        // liste de cibles côté moteur, pas de cible secondaire "soi-même" distincte
        // pour un coût), simplification à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.fardeau-partage", "Fardeau partagé",
            "Il absorbe le fardeau de l'allié.",
            "Heal", "SingleAlly", "Heal", mana: 10, power: 8, cancellationToken,
            category: "Magic", basePowerIsPercentOfMaxVitality: true);

        // "Berceuse inversée" : la doc cible Initiative (-4 brut) — Initiative n'est
        // qu'une valeur de départ figée pour l'ordre d'engagement côté moteur, pas un
        // canal de StatModifier modifiable en combat. Approximé par AtbTempoModifier
        // (ralentit directement le remplissage de la jauge ATB), même intention
        // (retarder les tours adverses) par un autre levier déjà câblé.
        await UpsertSkillAsync("canon.skill.berceuse-inversee", "Berceuse inversée",
            "Le sommeil monte sans qu'aucun son ne l'annonce.",
            "Debuff", "AllEnemies", "Debuff", mana: 12, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -15, TicksPerTurn * 3, Stat: "AtbTempoModifier") },
            category: "Magic");

        // "Note tenue" : la doc décrit un tour de canalisation puis des dégâts
        // doublés sur cible sous Silence — ni la canalisation (aucun sort ne prend
        // plus d'un tour côté moteur) ni le doublement conditionnel de puissance ne
        // sont câblés ; l'IA compense en ne visant que des cibles déjà sous Silence
        // (voir ChoeurMuetBossBehavior), même approche que Phrase inachevée
        // (famille Copistes).
        await UpsertSkillAsync("canon.skill.note-tenue", "Note tenue",
            "L'accord final.",
            "Damage", "SingleEnemy", "Damage", mana: 18, power: 20, cancellationToken,
            category: "Magic");

        await UpsertEnemyAsync(
            "canon.enemy.squelette-souvenir", "Squelette de Souvenir",
            "Un squelette gris cendre dont les os portent des gravures illisibles — les restes d'un moment que personne n'a jamais raconté. Il tient parfois un objet incongru : une tasse, un jouet, une clef. L'objet est le seul indice de ce qu'il fut. « ... » (il n'a jamais été raconté ; il n'a pas de voix)",
            "Skirmisher", family, "Common", "Skirmisher", isElite: false,
            depthMin: 3, depthMax: 8, riskMin: 1, riskMax: 4,
            roomTypes: new[] { "Silence", "Memory" },
            tags: new[] { "bestiaire", "memoire", "squelettes-de-souvenirs", "ossuaire" },
            skillKeys: new[] { "canon.skill.griffe-dos", "canon.skill.fragment-grave", "canon.skill.etreinte-creuse", "canon.skill.effondrement" },
            vitality: 34, attack: 8, defense: 6, guard: 0, speed: 7, focus: 2,
            cancellationToken,
            magicAttack: 3, magicDefense: 4, initiative: 6, mana: 8, menace: 2,
            rarity: "Common", registre: registre,
            boundRoomKeys: new[] { "room.enfer1" });

        await UpsertEnemyAsync(
            "canon.enemy.porteur-cendre", "Porteur de Cendre",
            "Une silhouette encapuchonnée courbée sous une hotte débordant de cendre et d'ossements. Elle traverse la Calamité en ramassant ce qui reste des souvenirs morts, et les rallume un à un, comme des braises. « Je me souviens d'eux. C'est mon fardeau, et ma monnaie. »",
            "Support", family, "Uncommon", "Support", isElite: false,
            depthMin: 3, depthMax: 8, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Silence", "Memory" },
            tags: new[] { "bestiaire", "memoire", "squelettes-de-souvenirs", "ossuaire", "priority" },
            skillKeys: new[] { "canon.skill.braise-memorielle", "canon.skill.jet-de-cendre", "canon.skill.fardeau-partage", "canon.skill.sursaut-memoriel" },
            vitality: 66, attack: 5, defense: 8, guard: 0, speed: 6, focus: 6,
            cancellationToken,
            magicAttack: 10, magicDefense: 9, initiative: 5, mana: 24, menace: 5,
            rarity: "Uncommon", registre: registre,
            boundRoomKeys: new[] { "room.enfer1" });

        await UpsertEnemyAsync(
            "canon.enemy.choeur-muet", "Chœur Muet",
            "Trois cages thoraciques fusionnées en un seul buste, surmontées de trois crânes aux mâchoires grandes ouvertes. Aucun son n'en sort — mais l'air vibre, et le silence qui règne autour d'eux pèse physiquement sur les épaules. « Ils chantent. Vous ne l'entendrez jamais. C'est ça, le supplice. »",
            "Disruptor", family, "Rare", "Disruptor", isElite: true,
            depthMin: 4, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Silence" },
            tags: new[] { "bestiaire", "silence", "squelettes-de-souvenirs", "elite", "control" },
            skillKeys: new[] { "canon.skill.lecture-des-silences", "canon.skill.silence", "canon.skill.berceuse-inversee", "canon.skill.note-tenue" },
            vitality: 74, attack: 4, defense: 7, guard: 0, speed: 4, focus: 8,
            cancellationToken,
            magicAttack: 13, magicDefense: 12, initiative: 3, mana: 26, menace: 6,
            rarity: "Rare", registre: "Silence",
            boundRoomKeys: new[] { "room.enfer1" });
    }

    private async Task SeedBestiaireChimeresDesPlainesAsync(CancellationToken cancellationToken)
    {
        const string family = "Chimeres des Plaines";
        const string registre = "Effroi";

        // Mécanique de famille "La Faim" (chaque dégât de DoT subi par un adversaire
        // charge un compteur partagé entre toutes les Chimères du combat, jusqu'à un
        // coup critique garanti à 5 crans) n'est pas modélisée — nécessiterait un
        // compteur partagé au niveau du Combat, inexistant côté moteur (même famille
        // de limitation que l'Ossuaire des Squelettes de Souvenirs). Les sorts et IA
        // ci-dessous approximent l'esprit de la mécanique (cible la plus couverte de
        // DoT) sans le compteur lui-même ni la garantie de critique. Le lifesteal de
        // Curée (soin de 50% des dégâts infligés) et l'intargetabilité de Bond de
        // flanc n'ont pas non plus de levier moteur dédié — approximés respectivement
        // par un soin en % des PV max et une frappe simple, documenté ci-dessous.

        await UpsertSkillAsync("canon.skill.morsure-composite", "Morsure composite",
            "Trois rangées de dents qui ne s'accordent pas.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 13, cancellationToken,
            category: "Physical");

        // "Bond de flanc" : la doc décrit une intargetabilité par les attaques de
        // mêlée jusqu'au prochain tour — aucun statut d'intargetabilité n'existe côté
        // moteur ; approximé par une frappe simple, simplification à assumer/affiner
        // plus tard.
        await UpsertSkillAsync("canon.skill.bond-de-flanc", "Bond de flanc",
            "Frappe et change de rang.",
            "Damage", "SingleEnemy", "Damage", mana: 6, power: 10, cancellationToken,
            category: "Physical");

        // "Curée" : la doc décrit un soin de 50% des dégâts infligés (lifesteal) —
        // aucun mécanisme de soin proportionnel aux dégâts d'un coup donné n'existe
        // côté moteur ; approximé par un soin en % des PV max du lanceur sur le
        // même coup, simplification à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.curee", "Curée",
            "Ne frappe que ce qui est déjà à terre.",
            "Damage", "SingleEnemy", "Damage", mana: 8, power: 16, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("HealOverTime", null, 8, TicksPerTurn, TickInterval: TicksPerTurn,
                    MagnitudeIsPercentOfMax: true, AppliesToActor: true)
            },
            category: "Physical");

        // "Guet" : la doc décrit +1 cran de Faim (non modélisé) et +15% esquive (aucun
        // levier d'esquive côté cible n'existe côté moteur) — approximé par une garde
        // instantanée, même esprit défensif par un autre levier déjà câblé.
        await UpsertSkillAsync("canon.skill.guet", "Guet",
            "Elle attend que ça saigne.",
            "Buff", "Self", "Guard", mana: 0, power: 6, cancellationToken,
            category: "Physical");

        // "Désignation" : la doc décrit +10% dégâts physiques subis — aucun canal
        // StatModifier générique "dégâts subis" n'existe côté moteur ; approximé par
        // une réduction de Défense (effet pratique équivalent via la formule de
        // dégâts symétrique), simplification à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.designation", "Désignation",
            "Marque une cible : toutes les Chimères la priorisent.",
            "Debuff", "SingleEnemy", "Debuff", mana: 6, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, -10, TicksPerTurn * 3, Stat: "Defense", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.houlette", "Houlette",
            "Un coup sec de la règle démesurée. Rappel à l'ordre.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 11, cancellationToken,
            category: "Physical");

        // "Ration" : la doc décrit une dépense de 2 crans de Faim (non modélisée) —
        // le coût est ignoré, seul le soin de groupe est câblé.
        await UpsertSkillAsync("canon.skill.ration", "Ration",
            "Le berger nourrit — un peu, jamais assez.",
            "Buff", "AllAllies", "Heal", mana: 10, power: 10, cancellationToken,
            category: "Magic", basePowerIsPercentOfMaxVitality: true);

        await UpsertSkillAsync("canon.skill.brout", "Brout",
            "Passe son tour en broutant. Le calme s'épaissit.",
            "Buff", "Self", "Guard", mana: 0, power: 8, cancellationToken,
            category: "Physical");

        // "Regard fixe" : la doc cible Initiative (-3 brut) — non modifiable en
        // combat côté moteur (voir la même note pour Berceuse inversée, famille
        // Squelettes de Souvenirs). Approximé par AtbTempoModifier, même intention.
        await UpsertSkillAsync("canon.skill.regard-fixe", "Regard fixe",
            "Vous l'avez regardé trop longtemps.",
            "Debuff", "SingleEnemy", "Debuff", mana: 6, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -12, TicksPerTurn * 3, Stat: "AtbTempoModifier") },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.belement-a-lenvers", "Bêlement à l'envers",
            "Un son qui rentre au lieu de sortir.",
            "Damage", "SingleEnemy", "Damage", mana: 10, power: 12, cancellationToken,
            category: "Magic");

        // "Détente" : la doc ajoute un déclenchement passif à la mort de l'Agneau —
        // aucun hook "on death" n'existe côté moteur (même famille de limitation que
        // l'Ossuaire) ; seul le déclenchement volontaire sous 25% PV est câblé, via
        // l'IA (AgneauInverseBossBehavior).
        await UpsertSkillAsync("canon.skill.detente", "Détente",
            "Le silence comprimé se libère.",
            "Damage", "AllEnemies", "Damage", mana: 0, power: 26, cancellationToken,
            effects: new[] { new SkillEffectSpec("Silence", null, 0, TicksPerTurn) },
            category: "Magic");

        await UpsertEnemyAsync(
            "canon.enemy.chimere-affamee", "Chimère Affamée",
            "Un prédateur composite — corps de cervidé, mâchoire de brochet, pattes trop nombreuses et repliées sous le ventre. Immobile dans les hautes herbes, elle est indiscernable des animaux paisibles de la plaine. Jusqu'à ce que quelque chose saigne. « Elle ne rugit pas. Elle compte vos battements de cœur. »",
            "Skirmisher", family, "Common", "Skirmisher", isElite: false,
            depthMin: 3, depthMax: 8, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "effroi", "chimeres-des-plaines", "faim" },
            skillKeys: new[] { "canon.skill.curee", "canon.skill.morsure-composite", "canon.skill.bond-de-flanc", "canon.skill.guet" },
            vitality: 52, attack: 12, defense: 5, guard: 0, speed: 13, focus: 2,
            cancellationToken,
            magicAttack: 2, magicDefense: 5, initiative: 12, mana: 8, menace: 4,
            rarity: "Common", registre: registre,
            boundRoomKeys: new[] { "room.enfer2" });

        await UpsertEnemyAsync(
            "canon.enemy.berger-ordres", "Berger d'Ordres",
            "Une haute figure pastorale au visage effacé, appuyée sur une houlette faite d'une règle d'architecte démesurément allongée. Il ne parle pas aux chimères : il leur montre, et elles comprennent. Ses gestes ont la précision d'un plan. « Le troupeau ne demande qu'une chose. Je la lui accorde. »",
            "Support", family, "Uncommon", "Support", isElite: false,
            depthMin: 3, depthMax: 8, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "folie", "chimeres-des-plaines", "faim", "priority" },
            skillKeys: new[] { "canon.skill.designation", "canon.skill.plongee-dans-la-folie", "canon.skill.houlette", "canon.skill.ration" },
            vitality: 70, attack: 6, defense: 7, guard: 0, speed: 7, focus: 8,
            cancellationToken,
            magicAttack: 11, magicDefense: 10, initiative: 8, mana: 24, menace: 6,
            rarity: "Uncommon", registre: "Folie",
            boundRoomKeys: new[] { "room.enfer2" });

        await UpsertEnemyAsync(
            "canon.enemy.agneau-inverse", "Agneau Inversé",
            "De loin : un agneau paisible, blanc, broutant. De près : la laine pousse vers l'intérieur, et ce qui remplit le corps n'est pas de la chair. C'est du silence comprimé, prêt à se détendre d'un coup. « Il broutait. Vous avez cligné des yeux. Il vous regarde. »",
            "Disruptor", family, "Uncommon", "Disruptor", isElite: false,
            depthMin: 3, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "effroi", "chimeres-des-plaines", "piege" },
            skillKeys: new[] { "canon.skill.brout", "canon.skill.regard-fixe", "canon.skill.belement-a-lenvers", "canon.skill.detente" },
            vitality: 40, attack: 5, defense: 6, guard: 0, speed: 8, focus: 4,
            cancellationToken,
            magicAttack: 7, magicDefense: 8, initiative: 7, mana: 14, menace: 3,
            rarity: "Uncommon", registre: registre,
            boundRoomKeys: new[] { "room.enfer2", "room.jardin" });
    }

    private async Task SeedBestiaireCreationsDuForgeronAsync(CancellationToken cancellationToken)
    {
        const string family = "Creations du Forgeron";
        const string registre = "Rupture";

        // Mécanique de famille "La Trempe" : quand une Création subit un buff
        // d'Attaque, elle gagne aussi +2 DEF (brut) pour la même durée. Câblée
        // directement dans les sorts eux-mêmes ci-dessous (Redressement,
        // Transmutation alliée, Litanie) plutôt que via une règle générique — chaque
        // sort qui buffe l'Attaque d'une Création porte aussi son propre effet +2 DEF.
        // Le second volet ("les DoT de feu posés par les Créations ne peuvent pas
        // être purgés tant que la Sentinelle de Fonte est en vie") est sans objet :
        // aucun mécanisme de purge/dissipation ciblée n'existe côté moteur (voir
        // Marge blanche, famille Copistes) — rien à immuniser.

        await UpsertSkillAsync("canon.skill.coup-de-plaque", "Coup de plaque",
            "Frappe avec ce qui dépasse.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 12, cancellationToken,
            category: "Physical");

        // "Foyer ouvert" : la doc décrit un piège réactif (Brûlure à la première
        // cible qui frappe au corps-à-corps avant le prochain tour) — aucun
        // déclencheur réactif "sur coup reçu" n'existe côté moteur (même famille de
        // limitation que l'Attitude en combat) ; approximé par une Brûlure lancée
        // activement, l'IA la réservant à l'agresseur le plus récent quand connu
        // (voir CreationInstableBossBehavior, via Combatant.LastAttackerId).
        await UpsertSkillAsync("canon.skill.foyer-ouvert", "Foyer ouvert",
            "Son torse s'ouvre.",
            "Debuff", "SingleEnemy", "Debuff", mana: 8, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("DamageOverTime", null, 5, TicksPerTurn * 4, TickInterval: TicksPerTurn) },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.redressement", "Redressement",
            "Elle se remet droite. Encore.",
            "Buff", "Self", "Buff", mana: 6, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.redressement:attack", 4, TicksPerTurn * 3, Stat: "AttackPower"),
                new SkillEffectSpec("StatModifier", "canon.skill.redressement:trempe", 2, TicksPerTurn * 3, Stat: "Defense")
            },
            category: "Physical");

        await UpsertSkillAsync("canon.skill.frappe-denclume", "Frappe d'enclume",
            "Le geste appris. Précis, cadencé, sans intention.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 14, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.cadence", "Cadence",
            "Le rythme s'accélère.",
            "Buff", "Self", "Buff", mana: 8, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, 10, TicksPerTurn * 4, Stat: "Speed", MagnitudeIsPercentOfBaseStat: true) },
            category: "Physical");

        // "Coup de grâce du forgeron" : la doc décrit la consommation immédiate du
        // DoT restant sur la cible — aucun mécanisme de lecture/consommation d'un
        // DoT actif n'existe côté moteur ; approximé par une puissance élevée fixe,
        // l'IA le réservant aux cibles déjà sous DoT (voir MarteauVivantBossBehavior).
        await UpsertSkillAsync("canon.skill.coup-de-grace-forgeron", "Coup de grâce du forgeron",
            "On frappe le fer tant qu'il est chaud.",
            "Damage", "SingleEnemy", "Damage", mana: 14, power: 22, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.transmutation-alliee", "Transmutation",
            "Plomb, or, mercure, soufre, sel.",
            "Buff", "SingleAlly", "Buff", mana: 6, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.transmutation-alliee:attack", 4, TicksPerTurn * 3, Stat: "AttackPower"),
                new SkillEffectSpec("StatModifier", "canon.skill.transmutation-alliee:trempe", 2, TicksPerTurn * 3, Stat: "Defense")
            },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.litanie", "Litanie",
            "La formule récitée en entier, une fois n'est pas coutume.",
            "Buff", "AllAllies", "Buff", mana: 10, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.litanie:attack", 4, TicksPerTurn * 2, Stat: "AttackPower"),
                new SkillEffectSpec("StatModifier", "canon.skill.litanie:trempe", 2, TicksPerTurn * 2, Stat: "Defense")
            },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.scorie", "Scorie",
            "Crache du métal en fusion.",
            "Damage", "SingleEnemy", "Damage", mana: 8, power: 11, cancellationToken,
            effects: new[] { new SkillEffectSpec("DamageOverTime", null, 4, TicksPerTurn * 3, TickInterval: TicksPerTurn) },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.fonte", "Fonte",
            "Elle était déjà assise.",
            "Buff", "Self", "Buff", mana: 12, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, 8, TicksPerTurn * 3, Stat: "Defense"),
                new SkillEffectSpec("StatModifier", null, -2, TicksPerTurn * 3, Stat: "Speed")
            },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.contact", "Contact",
            "Toucher incandescent.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 8, cancellationToken,
            category: "Physical");

        // "Laitier ardent" : la doc décrit +2 tours flat sur les DoT de feu actifs —
        // réutilise l'EffectType "ExtendDotDuration" déjà câblé (Écriture continuelle,
        // famille Copistes), qui n'étend qu'en pourcentage de la durée restante, pas
        // en tours fixes ; approximé par +40%.
        await UpsertSkillAsync("canon.skill.laitier-ardent", "Laitier ardent",
            "La braise recouverte dure plus longtemps.",
            "Debuff", "SingleEnemy", "ExtendDotDuration", mana: 8, power: 40, cancellationToken,
            category: "Magic");

        // "Éclat vitrifié" : la doc décrit 10% de chance d'appliquer Brûlure — aucune
        // application d'effet probabiliste n'existe côté moteur (un effet attaché à
        // un sort s'applique systématiquement au toucher) ; la Brûlure s'applique
        // donc à chaque coup, simplification à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.eclat-vitrifie", "Éclat vitrifié",
            "Projette un fragment.",
            "Damage", "SingleEnemy", "Damage", mana: 5, power: 9, cancellationToken,
            effects: new[] { new SkillEffectSpec("DamageOverTime", null, 3, TicksPerTurn * 3, TickInterval: TicksPerTurn) },
            category: "Physical");

        // "Reformation" : la doc limite l'usage à 2 fois par combat — aucun compteur
        // d'utilisations par sort n'existe côté moteur (au-delà du coût en
        // mana/charge) ; utilisable sans limite, simplification à assumer/affiner
        // plus tard.
        await UpsertSkillAsync("canon.skill.reformation", "Reformation",
            "Se reforme.",
            "Buff", "Self", "Heal", mana: 6, power: 15, cancellationToken,
            category: "Magic", basePowerIsPercentOfMaxVitality: true);

        await UpsertEnemyAsync(
            "canon.enemy.creation-instable", "Création Instable",
            "Un assemblage humanoïde de plaques mal jointes, dont une jambe est plus courte que l'autre et dont le torse s'ouvre par intermittence sur un foyer qui n'aurait jamais dû rester allumé. Elle se redresse sans cesse, compulsivement, comme pour prouver quelque chose à un marteau absent. « Elle se tient debout. Presque. C'est le presque qui fait mal. »",
            "Bruiser", family, "Common", "Bruiser", isElite: false,
            depthMin: 4, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "rupture", "creations-du-forgeron", "trempe" },
            skillKeys: new[] { "canon.skill.coup-de-plaque", "canon.skill.egide", "canon.skill.foyer-ouvert", "canon.skill.redressement" },
            vitality: 78, attack: 12, defense: 9, guard: 0, speed: 6, focus: 3,
            cancellationToken,
            magicAttack: 3, magicDefense: 5, initiative: 5, mana: 12, menace: 4,
            rarity: "Common", registre: registre,
            boundRoomKeys: new[] { "room.enfer3" });

        await UpsertEnemyAsync(
            "canon.enemy.marteau-vivant", "Marteau Vivant",
            "Un marteau de forge de deux mètres, animé, dont le manche s'est tordu en colonne vertébrale. Il frappe le sol en rythme, continuellement — le rythme exact du Forgeron au travail. Quand il frappe autre chose que le sol, ça hurle. C'est lui, le hurlement. « Les marteaux qui hurlent. C'est de lui qu'on parle. »",
            "Bruiser", family, "Uncommon", "Bruiser", isElite: false,
            depthMin: 4, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "rupture", "creations-du-forgeron", "dot" },
            skillKeys: new[] { "canon.skill.frappe-denclume", "canon.skill.souffle-de-la-forge", "canon.skill.cadence", "canon.skill.coup-de-grace-forgeron" },
            vitality: 64, attack: 14, defense: 7, guard: 0, speed: 8, focus: 3,
            cancellationToken,
            magicAttack: 6, magicDefense: 4, initiative: 7, mana: 18, menace: 6,
            rarity: "Uncommon", registre: registre,
            boundRoomKeys: new[] { "room.enfer3" });

        await UpsertEnemyAsync(
            "canon.enemy.sentinelle-fonte", "Sentinelle de Fonte",
            "Une statue de fonte grossière, assise en tailleur au milieu des piliers de fer, qui murmure la litanie alchimique du Forgeron. Elle ne se lève jamais. Ses mains, posées sur ses genoux, rougissent quand elle transmute — et le métal de ses alliés rougit avec. « Plomb, or, mercure, soufre, sel. Elle récite. C'est tout ce qu'on lui a laissé. »",
            "Support", family, "Uncommon", "Support", isElite: false,
            depthMin: 4, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "rupture", "creations-du-forgeron", "trempe", "priority" },
            skillKeys: new[] { "canon.skill.transmutation-alliee", "canon.skill.litanie", "canon.skill.scorie", "canon.skill.fonte" },
            vitality: 82, attack: 5, defense: 13, guard: 0, speed: 3, focus: 6,
            cancellationToken,
            magicAttack: 9, magicDefense: 8, initiative: 2, mana: 22, menace: 5,
            rarity: "Uncommon", registre: registre,
            boundRoomKeys: new[] { "room.enfer3" });

        await UpsertEnemyAsync(
            "canon.enemy.scorie-rampante", "Scorie Rampante",
            "Une flaque de laitier incandescent, à demi solidifiée, qui se traîne en laissant des traces vitrifiées. Par moments, une forme s'ébauche dans sa masse — une main, un profil — puis retombe. Elle n'a jamais eu de forme finale. Elle les essaie toutes. « Ce que la forge recrache. Ça rampe. Ça brûle. Ça se souvient d'avoir été un projet. »",
            "Skirmisher", family, "Common", "Skirmisher", isElite: false,
            depthMin: 4, depthMax: 10, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "rupture", "creations-du-forgeron", "dot" },
            skillKeys: new[] { "canon.skill.contact", "canon.skill.laitier-ardent", "canon.skill.eclat-vitrifie", "canon.skill.reformation" },
            vitality: 30, attack: 7, defense: 4, guard: 0, speed: 4, focus: 2,
            cancellationToken,
            magicAttack: 6, magicDefense: 6, initiative: 3, mana: 10, menace: 2,
            rarity: "Common", registre: registre,
            boundRoomKeys: new[] { "room.enfer3", "room.enfer4" });
    }

    private async Task SeedBestiaireBlousesBlanchesAsync(CancellationToken cancellationToken)
    {
        const string family = "Blouses Blanches";
        const string registre = "Deni";

        // Mécanique de famille "Le Dossier" (la première fois qu'un adversaire utilise
        // chaque type d'action — attaque physique, sort magique, soin, buff — les
        // Blouses le "consignent" ; entièrement consigné, il subit -10% sur toutes ses
        // statistiques) n'est pas modélisée — nécessiterait un suivi partagé, par
        // adversaire, des catégories d'action déjà observées, absent du moteur (même
        // famille de limitation que l'Ossuaire/la Faim/la Résonance des familles
        // précédentes).

        // "Placebo" : la doc décrit la négation du prochain soin reçu par la cible
        // (converti en 0, récupéré en Garde par l'Infirmière) — aucun mécanisme
        // d'interception d'un soin futur n'existe côté moteur ; approximé par une
        // garde instantanée directe, simplification à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.placebo", "Placebo",
            "Le prochain soin reçu par la cible est nié.",
            "Buff", "Self", "Guard", mana: 10, power: 10, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.bordage", "Bordage",
            "C'est pour votre bien.",
            "Debuff", "SingleEnemy", "Debuff", mana: 12, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.bordage:speed", -15, TicksPerTurn * 3, Stat: "Speed", MagnitudeIsPercentOfBaseStat: true),
                new SkillEffectSpec("StatModifier", "canon.skill.bordage:attack", -10, TicksPerTurn * 3, Stat: "AttackPower", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Magic");

        // "Injection blanche" : la doc ajoute la purge d'1 buff de la cible — aucun
        // mécanisme de dissipation ciblée n'existe côté moteur (voir Marge blanche,
        // famille Copistes) ; dégâts purs, simplification à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.injection-blanche", "Injection blanche",
            "Le produit fait effet : vous redevenez conforme.",
            "Damage", "SingleEnemy", "Damage", mana: 8, power: 10, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.drap-tendu", "Drap tendu",
            "Le tissu sent le produit ménager.",
            "Debuff", "SingleEnemy", "Debuff", mana: 6, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -3, TicksPerTurn * 3, Stat: "Focus") },
            category: "Physical");

        // "Sonnette" : la doc décrit un appel qui fait jouer un Placebo gratuit à
        // l'Infirmière si présente — aucun mécanisme ne permet de déclencher l'action
        // d'un autre combattant hors de son tour côté moteur ; seul le repli ("sinon
        // +5 Garde sur soi") est câblé.
        await UpsertSkillAsync("canon.skill.sonnette", "Sonnette",
            "Appelle.",
            "Buff", "Self", "Guard", mana: 8, power: 5, cancellationToken,
            category: "Magic");

        // "Visite" : la doc décrit une attraction de rang — aucun système de rang
        // n'existe côté moteur (ciblage plat) ; dégâts seuls, simplification à
        // assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.visite", "Visite",
            "Approchez. Il attendait.",
            "Damage", "SingleEnemy", "Damage", mana: 12, power: 14, cancellationToken,
            category: "Magic");

        // "Tour de clef" : la doc décrit une immobilisation de rang/fuite — sans objet
        // côté moteur (pas de rang, pas d'action de fuite) ; approximé par un débuff
        // d'Attaque (la cible "enfermée" perd en initiative offensive), simplification
        // à assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.tour-de-clef", "Tour de clef",
            "La chambre est fermée.",
            "Debuff", "SingleEnemy", "Debuff", mana: 14, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, -10, TicksPerTurn * 2, Stat: "AttackPower", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.trousseau", "Trousseau",
            "Le poids de toutes les portes.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 13, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.extinction-des-feux", "Extinction des feux",
            "Le règlement est appliqué.",
            "Damage", "SingleEnemy", "Damage", mana: 20, power: 24, cancellationToken,
            category: "Magic");

        await UpsertEnemyAsync(
            "canon.enemy.infirmiere-deni", "Infirmière du Déni",
            "Une silhouette amidonnée, impeccable, dont la coiffe descend trop bas pour qu'on voie les yeux. Elle pousse un chariot dont les fioles sont toutes étiquetées du même mot, illisible. Sa voix est celle de Margot — en plus douce, ce qui est pire. « Vous n'avez pas mal. Regardez le dossier : nulle part il n'est écrit que vous avez mal. »",
            "Disruptor", family, "Uncommon", "Disruptor", isElite: false,
            depthMin: 4, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "deni", "blouses-blanches", "dossier" },
            skillKeys: new[] { "canon.skill.placebo", "canon.skill.injection-blanche", "canon.skill.bordage", "canon.skill.anagramme" },
            vitality: 68, attack: 4, defense: 6, guard: 0, speed: 8, focus: 8,
            cancellationToken,
            magicAttack: 12, magicDefense: 11, initiative: 8, mana: 26, menace: 6,
            rarity: "Uncommon", registre: registre,
            boundRoomKeys: new[] { "room.hopital", "room.cellulehopital" });

        await UpsertEnemyAsync(
            "canon.enemy.souvenir-alite", "Souvenir Alité",
            "Un lit d'hôpital qui se déplace seul, draps tendus sur une forme humaine qui respire. Personne n'est dessous. La forme respire quand même. Sur la table de chevet, des fleurs fanées se refont une jeunesse quand on les regarde. « Il attend une visite. Vous ferez l'affaire. »",
            "Skirmisher", family, "Common", "Skirmisher", isElite: false,
            depthMin: 4, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "melancolie", "blouses-blanches", "dot" },
            skillKeys: new[] { "canon.skill.nevrose", "canon.skill.sonnette", "canon.skill.visite", "canon.skill.drap-tendu" },
            vitality: 56, attack: 6, defense: 8, guard: 0, speed: 4, focus: 5,
            cancellationToken,
            magicAttack: 9, magicDefense: 9, initiative: 3, mana: 20, menace: 3,
            rarity: "Common", registre: "Melancolie",
            boundRoomKeys: new[] { "room.hopital" });

        await UpsertEnemyAsync(
            "canon.enemy.regisseur-blanc", "Régisseur des Couloirs Blancs",
            "Un fonctionnaire immense au dos droit, dont le trousseau de clefs pend jusqu'au sol. Chaque clef ouvre une porte qui n'existe plus. Il arpente les couloirs blancs en vérifiant des serrures absentes, et l'ordre qu'il maintient est si total que l'air lui-même circule en file indienne. « Les visites sont terminées. Elles l'ont toujours été. »",
            "Support", family, "Rare", "Support", isElite: true,
            depthMin: 5, depthMax: 9, riskMin: 2, riskMax: 5,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "deni", "blouses-blanches", "elite", "control" },
            skillKeys: new[] { "canon.skill.contemplation-infinie", "canon.skill.tour-de-clef", "canon.skill.trousseau", "canon.skill.extinction-des-feux" },
            vitality: 96, attack: 11, defense: 11, guard: 0, speed: 5, focus: 6,
            cancellationToken,
            magicAttack: 9, magicDefense: 10, initiative: 4, mana: 22, menace: 7,
            rarity: "Rare", registre: registre,
            boundRoomKeys: new[] { "room.hopital", "room.cellulehopital" });
    }

    private async Task SeedBestiairePenitentsDeLaMontagneAsync(CancellationToken cancellationToken)
    {
        const string family = "Penitents de la Montagne";
        const string registre = "Effroi";

        // Mécanique de famille "Le Pèlerinage" (chaque Prière pose une Station, max 3 ;
        // à 3 Stations, la Frayeur Exhumée peut se manifester en renfort, ou les
        // Pénitents se soignent de 20% PV max) n'est pas modélisée — nécessiterait un
        // compteur partagé au niveau du Combat ET une invocation de renfort en cours
        // de combat, absents du moteur (même famille de limitation que les mécaniques
        // de famille précédentes). "Chapelet de dents" perd son volet "+2 dégâts par
        // Station" (puissance fixe) ; "Dernière prière" perd son volet "pose 2
        // Stations d'un coup" et devient une Prière renforcée à la place.

        await UpsertSkillAsync("canon.skill.baton-de-marche", "Bâton de marche",
            "Un coup du bâton poli par des siècles de pente.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 9, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.chapelet-de-dents", "Chapelet de dents",
            "Égrène.",
            "Damage", "SingleEnemy", "Damage", mana: 8, power: 11, cancellationToken,
            category: "Magic");

        // "Repentir" : la doc décrit -10% PV max sur soi en plus du buff — la vitalité
        // maximale d'un combattant est une valeur figée à la création côté moteur,
        // aucune réduction en cours de combat n'est possible ; seul le buff est câblé.
        await UpsertSkillAsync("canon.skill.repentir", "Repentir",
            "La faute oubliée exige quand même son prix.",
            "Buff", "Self", "Buff", mana: 6, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, 6, TicksPerTurn * 3, Stat: "MagicAttack") },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.encens-inverse", "Encens inversé",
            "Ce qui rôde respire mieux.",
            "Debuff", "AllEnemies", "Debuff", mana: 10, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -3, TicksPerTurn * 3, Stat: "MagicDefense") },
            category: "Magic");

        // "Oraison cousue" : la doc décrit un soin de 40% des dégâts infligés
        // (lifesteal conditionnel) — aucun mécanisme de soin proportionnel aux dégâts
        // d'un coup donné n'existe côté moteur ; approximé par un soin en % des PV max
        // fixe sur le même coup. L'IA ne la lance que sur une cible déjà sous -DEF
        // (voir PrieurLituiqueBossBehavior), préservant le synergisme documenté même
        // sans le pourcentage conditionnel.
        await UpsertSkillAsync("canon.skill.oraison-cousue", "Oraison cousue",
            "Le drain accompli.",
            "Damage", "SingleEnemy", "Damage", mana: 14, power: 18, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("HealOverTime", null, 10, TicksPerTurn, TickInterval: TicksPerTurn,
                    MagnitudeIsPercentOfMax: true, AppliesToActor: true)
            },
            category: "Magic");

        // "Dernière prière" : voir la note de mécanique de famille ci-dessus — devient
        // une Prière renforcée (débuff de Défense doublé) plutôt que de poser deux
        // Stations. Le plafond "une fois par combat" n'est pas non plus appliqué
        // (aucun compteur d'utilisation par sort côté moteur).
        await UpsertSkillAsync("canon.skill.derniere-priere", "Dernière prière",
            "Le nom exact de ce que Him'Lit n'aime pas.",
            "Drain", "SingleEnemy", "Debuff", mana: 20, power: 18, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -8, TicksPerTurn * 3, Stat: "Defense") },
            category: "Magic");

        // "Posture finale" : la doc cible Initiative (-4 brut) — non modifiable en
        // combat côté moteur (même substitution que Berceuse inversée/Regard fixe) ;
        // approximée par AtbTempoModifier.
        await UpsertSkillAsync("canon.skill.posture-finale", "Posture finale",
            "Elle a vu.",
            "Debuff", "SingleEnemy", "Debuff", mana: 10, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", "canon.skill.posture-finale:tempo", -12, TicksPerTurn * 3, Stat: "AtbTempoModifier"),
                new SkillEffectSpec("StatModifier", "canon.skill.posture-finale:focus", -3, TicksPerTurn * 3, Stat: "Focus")
            },
            category: "Magic");

        // "Griffe de recul" : la doc décrit une intargetabilité au corps-à-corps —
        // sans objet côté moteur (aucun statut d'intargetabilité, voir Bond de flanc,
        // famille Chimères) ; dégâts seuls.
        await UpsertSkillAsync("canon.skill.griffe-de-recul", "Griffe de recul",
            "Frappe en reculant.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 14, cancellationToken,
            category: "Physical");

        await UpsertEnemyAsync(
            "canon.enemy.pelerin-sans-visage", "Pèlerin Sans Visage",
            "Une silhouette en robe de bure, courbée par la pente, dont la capuche s'ouvre sur une surface lisse — pas effacée : usée, comme une pièce de monnaie trop manipulée. Il gravit la montagne en égrenant un chapelet dont chaque grain est une petite dent. « Il monte depuis si longtemps qu'il a usé son visage contre le vent. »",
            "Skirmisher", family, "Common", "Skirmisher", isElite: false,
            depthMin: 4, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory", "Silence" },
            tags: new[] { "bestiaire", "melancolie", "penitents-de-la-montagne", "pelerinage" },
            skillKeys: new[] { "canon.skill.priere-aspiration", "canon.skill.baton-de-marche", "canon.skill.chapelet-de-dents", "canon.skill.repentir" },
            vitality: 42, attack: 7, defense: 6, guard: 0, speed: 6, focus: 5,
            cancellationToken,
            magicAttack: 7, magicDefense: 8, initiative: 5, mana: 16, menace: 3,
            rarity: "Common", registre: "Melancolie",
            boundRoomKeys: new[] { "room.montagne", "room.templempontagne" });

        await UpsertEnemyAsync(
            "canon.enemy.prieur-lituique", "Prieur Lituique",
            "Un officiant au dos trop droit pour la bure qu'il porte, dont la bouche est cousue de fil d'or — et qui prie quand même, par les pores, par les gestes, par les jointures de ses doigts qui craquent en rythme liturgique. Devant lui flotte un encensoir qui fume à l'envers : la fumée descend. « Elle restaure — mais nourrit ce qui rôde. Lui, il sait exactement ce qui rôde. »",
            "Support", family, "Uncommon", "Support", isElite: false,
            depthMin: 4, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "effroi", "penitents-de-la-montagne", "pelerinage", "priority" },
            skillKeys: new[] { "canon.skill.priere-aspiration", "canon.skill.encens-inverse", "canon.skill.oraison-cousue", "canon.skill.derniere-priere" },
            vitality: 72, attack: 5, defense: 8, guard: 0, speed: 6, focus: 8,
            cancellationToken,
            magicAttack: 13, magicDefense: 11, initiative: 6, mana: 28, menace: 6,
            rarity: "Uncommon", registre: registre,
            boundRoomKeys: new[] { "room.templempontagne", "room.chambrefunéraire" });

        await UpsertEnemyAsync(
            "canon.enemy.frayeur-exhumee", "Frayeur Exhumée",
            "Le premier explorateur — ou ce que l'ouverture de sa chambre funéraire a réveillé de lui. Un corps momifié dans une posture de recul, bras levés devant un danger que personne d'autre ne voit, figé au centième de seconde de sa dernière terreur. Il projette cette terreur autour de lui comme une lampe projette la lumière. « Depuis la découverte de la chambre, les échos de la frayeur ne cessent de s'agiter. En voici la source. »",
            "Bruiser", family, "Rare", "Bruiser", isElite: true,
            depthMin: 5, depthMax: 9, riskMin: 2, riskMax: 5,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "effroi", "penitents-de-la-montagne", "elite", "dot" },
            skillKeys: new[] { "canon.skill.frayeur-organique", "canon.skill.posture-finale", "canon.skill.griffe-de-recul", "canon.skill.nevrose" },
            vitality: 104, attack: 12, defense: 8, guard: 0, speed: 7, focus: 6,
            cancellationToken,
            magicAttack: 14, magicDefense: 9, initiative: 9, mana: 24, menace: 8,
            rarity: "Rare", registre: registre,
            boundRoomKeys: new[] { "room.chambrefunéraire", "room.sousterrainmontagne" });
    }

    private async Task SeedBestiaireFauxHabitantsDuJardinAsync(CancellationToken cancellationToken)
    {
        const string family = "Faux Habitants du Jardin";
        const string registre = "Deni";

        // Mécanique de famille "La Boucle" (les Faux Habitants rejouent leur premier
        // tour de combat tous les 3 tours — même sort, même cible) n'est pas modélisée
        // littéralement : rejouer une décision passée exigerait de mémoriser le tour 1
        // (sort + cible) quelque part, or IBossBehavior.DecideAction est sans état
        // (relit uniquement le Combat/Combattant courants à chaque appel), et rien ne
        // conserve cet historique côté moteur. Approximée par un cycle pondéré
        // toujours actif plutôt qu'une vraie boucle mémorisée.

        await UpsertSkillAsync("canon.skill.salut-de-chapeau", "Salut de chapeau",
            "Le bord du chapeau est une lame.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 10, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.conversation-tranquille", "Conversation tranquille",
            "Ça n'a ni début ni fin.",
            "Debuff", "SingleEnemy", "Debuff", mana: 8, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -3, TicksPerTurn * 3, Stat: "Focus") },
            category: "Magic");

        // "Pas de promenade" : la doc décrit un changement de rang + 15% esquive —
        // sans objet côté moteur (pas de rang, pas de levier d'esquive côté cible) ;
        // approximé par une garde instantanée.
        await UpsertSkillAsync("canon.skill.pas-de-promenade", "Pas de promenade",
            "Toutes les quarante secondes, exactement.",
            "Buff", "Self", "Guard", mana: 4, power: 6, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.sifflotement", "Sifflotement",
            "Le souffle bouclé, projeté.",
            "Damage", "AllEnemies", "Damage", mana: 6, power: 8, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.secateur", "Sécateur",
            "Il taille les tiges et les tendons avec le même soin.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 13, cancellationToken,
            category: "Physical");

        // "Émondage" : la doc ajoute la purge de TOUS les buffs de la cible — aucun
        // mécanisme de dissipation n'existe côté moteur (voir Marge blanche, famille
        // Copistes) ; dégâts seuls.
        await UpsertSkillAsync("canon.skill.emondage", "Émondage",
            "Ce qui dépasse est coupé.",
            "Damage", "SingleEnemy", "Damage", mana: 10, power: 11, cancellationToken,
            category: "Physical");

        // "Greffe" : la doc décrit le vol du dernier buff purgé, réappliqué à un
        // allié — dépend d'Émondage, qui ne purge pas réellement côté moteur ;
        // devient un buff de Défense direct sur l'allié, simplification à
        // assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.greffe", "Greffe",
            "Rien ne se perd au jardin.",
            "Buff", "SingleAlly", "Buff", mana: 12, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, 15, TicksPerTurn * 3, Stat: "Defense", MagnitudeIsPercentOfBaseStat: true) },
            category: "Magic");

        // "Paillage" : la doc ajoute une immunité au prochain débuff — aucun statut
        // d'immunité n'existe côté moteur ; seule la garde est câblée.
        await UpsertSkillAsync("canon.skill.paillage", "Paillage",
            "Le massif est protégé pour l'hiver.",
            "Buff", "SingleAlly", "Guard", mana: 8, power: 8, cancellationToken,
            category: "Magic");

        await UpsertEnemyAsync(
            "canon.enemy.promeneur-fige", "Promeneur Figé",
            "Un promeneur en habits du dimanche, sourire cordial, chapeau levé en salut perpétuel. Son bras ne redescend jamais complètement. Quand on le croise une deuxième fois, il salue exactement pareil — même angle, même sourire, même phrase, même virgule. « Belle journée, n'est-ce pas ? N'est-ce pas ? N'est-ce pas ? »",
            "Skirmisher", family, "Common", "Skirmisher", isElite: false,
            depthMin: 3, depthMax: 8, riskMin: 1, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "deni", "faux-habitants-du-jardin", "boucle" },
            skillKeys: new[] { "canon.skill.salut-de-chapeau", "canon.skill.conversation-tranquille", "canon.skill.pas-de-promenade", "canon.skill.sifflotement" },
            vitality: 38, attack: 8, defense: 5, guard: 0, speed: 7, focus: 3,
            cancellationToken,
            magicAttack: 5, magicDefense: 6, initiative: 6, mana: 10, menace: 2,
            rarity: "Common", registre: registre,
            boundRoomKeys: new[] { "room.jardin" });

        await UpsertEnemyAsync(
            "canon.enemy.jardinier-sans-ombre", "Jardinier Sans Ombre",
            "Un jardinier voûté sur ses massifs, sécateur en main, qui taille sans interruption des fleurs déjà parfaites. Le soleil du Palais l'éclaire de face, de dos, de partout — et il ne projette aucune ombre. C'est lui qui l'a coupée : elle faisait désordre. « Les fleurs sont merveilleuses parce que je coupe tout ce qui ne l'est pas. »",
            "Disruptor", family, "Uncommon", "Disruptor", isElite: false,
            depthMin: 4, depthMax: 9, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "deni", "faux-habitants-du-jardin", "anti-buff" },
            skillKeys: new[] { "canon.skill.emondage", "canon.skill.greffe", "canon.skill.secateur", "canon.skill.paillage" },
            vitality: 74, attack: 12, defense: 7, guard: 0, speed: 9, focus: 5,
            cancellationToken,
            magicAttack: 7, magicDefense: 8, initiative: 9, mana: 18, menace: 5,
            rarity: "Uncommon", registre: registre,
            boundRoomKeys: new[] { "room.jardin", "room.soleil" });
    }

    private async Task SeedBestiaireGardiensDeCrystalAsync(CancellationToken cancellationToken)
    {
        const string family = "Gardiens de Crystal";
        const string registre = "Memoire";

        // Mécanique de famille "La Résonance" (chaque dégât magique subi par un
        // Gardien charge un compteur partagé entre tous les Gardiens du combat,
        // jusqu'à +1 cible par palier de 3) n'est pas modélisée — nécessiterait un
        // compteur partagé au niveau du Combat, absent du moteur (même famille de
        // limitation que la Faim/le Pèlerinage/le Dossier). "Réfraction" perd donc
        // son volet multi-cible ; "Pulsation" perd son incrément de Résonance.

        await UpsertSkillAsync("canon.skill.poing-de-crystal", "Poing de crystal",
            "Le poids d'un âge entier dans un seul coup.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 17, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.refraction", "Réfraction",
            "Renvoie la lumière ancestrale.",
            "Damage", "SingleEnemy", "Damage", mana: 12, power: 15, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.stase", "Stase",
            "Elle rejoint, un instant, les objets en suspension.",
            "Debuff", "SingleEnemy", "Debuff", mana: 18, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("Silence", "canon.skill.stase:silence", 0, TicksPerTurn * 2),
                new SkillEffectSpec("StatModifier", "canon.skill.stase:speed", -25, TicksPerTurn * 2, Stat: "Speed", MagnitudeIsPercentOfBaseStat: true)
            },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.facette", "Facette",
            "La lumière cherche l'angle.",
            "Buff", "Self", "Buff", mana: 8, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, 15, TicksPerTurn * 2, Stat: "MagicDamageBonus") },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.pulsation", "Pulsation",
            "Le battement se propage.",
            "Damage", "SingleEnemy", "Damage", mana: 10, power: 12, cancellationToken,
            category: "Magic");

        // "Prisme" : la doc décrit une répartition des dégâts du prochain sort
        // mono-cible subi entre les Gardiens — aucun mécanisme de redirection/partage
        // de dégâts entrants n'existe côté moteur (voir Reliure de chair, famille
        // Copistes) ; approximé par une garde instantanée, simplification à
        // assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.prisme", "Prisme",
            "La lumière ne s'arrête pas, elle se partage.",
            "Buff", "Self", "Guard", mana: 14, power: 12, cancellationToken,
            category: "Magic");

        await UpsertEnemyAsync(
            "canon.enemy.gardien-intemporel", "Gardien Intemporel",
            "Un colosse de crystal translucide dans lequel on distingue, en suspension, des objets d'époques impossibles : un marteau qui n'est pas celui du Forgeron, une craie qui n'est pas celle de l'Enfant, une plume qui n'est pas celle de l'Écrivain. Des prototypes. Ou des originaux. « Il gardait déjà. Il gardera encore. Le mot “toujours” a été inventé pour éviter de le décrire. »",
            "Bruiser", family, "Rare", "Bruiser", isElite: true,
            depthMin: 5, depthMax: 9, riskMin: 3, riskMax: 5,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "memoire-ancienne", "gardiens-de-crystal", "elite", "resonance" },
            skillKeys: new[] { "canon.skill.poing-de-crystal", "canon.skill.rempart", "canon.skill.refraction", "canon.skill.stase" },
            vitality: 130, attack: 14, defense: 14, guard: 0, speed: 3, focus: 5,
            cancellationToken,
            magicAttack: 10, magicDefense: 13, initiative: 2, mana: 26, menace: 8,
            rarity: "Rare", registre: registre,
            boundRoomKeys: new[] { "room.sousterrainmontagne", "room.cavernedecrystal" });

        await UpsertEnemyAsync(
            "canon.enemy.eclat-eveille", "Éclat Éveillé",
            "Un cristal flottant de la taille d'un cœur, qui pulse d'une lumière interne au rythme d'un battement. Il n'a ni yeux ni bouche, mais tous ceux qui l'approchent jurent s'être sentis dévisagés — puis mémorisés. « Un joyau qui a fini par comprendre qu'on le regardait. »",
            "Skirmisher", family, "Uncommon", "Skirmisher", isElite: false,
            depthMin: 5, depthMax: 9, riskMin: 2, riskMax: 5,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "memoire-ancienne", "gardiens-de-crystal", "resonance" },
            skillKeys: new[] { "canon.skill.flamme-seraphine", "canon.skill.facette", "canon.skill.pulsation", "canon.skill.prisme" },
            vitality: 44, attack: 3, defense: 4, guard: 0, speed: 10, focus: 9,
            cancellationToken,
            magicAttack: 15, magicDefense: 12, initiative: 10, mana: 30, menace: 5,
            rarity: "Uncommon", registre: registre,
            boundRoomKeys: new[] { "room.cavernedecrystal" });
    }

    private async Task SeedBestiaireEchosDEmotionsAsync(CancellationToken cancellationToken)
    {
        const string family = "Echos d'Emotions";

        // Mécanique de famille "La Dissonance" (deux Échos différents dans le même
        // combat s'amplifient de +10% M.ATQ chacun ; deux Échos identiques se
        // parasitent à -10% chacun) n'est pas modélisée — appliquer un tel bonus
        // dépendrait de la composition exacte du combat au moment de l'engagement,
        // et bien qu'un premier tour puisse en théorie le calculer, l'effort de
        // nouvelle autorisation (deux sorts de buff/debuff dédiés rien que pour ce
        // calcul d'ouverture) n'a pas été jugé prioritaire face au reste du
        // Bestiaire restant — différé, comme les autres mécaniques de famille à
        // compteur partagé. La clause "un Écho n'apparaît jamais dans un combat où
        // son Émotion originale est scriptée en événement" est hors du périmètre du
        // moteur de combat (c'est une règle de sélection de rencontre, pas de
        // comportement en combat) — non câblée non plus.

        await UpsertSkillAsync("canon.skill.eclat-echo-colere", "Éclat",
            "Le poing retombe, enfin.",
            "Damage", "SingleEnemy", "Damage", mana: 0, power: 12, cancellationToken,
            category: "Physical");

        // "Constat sec" : la doc décrit +15% dégâts DE CET ÉCHO SPÉCIFIQUEMENT sur la
        // cible désignée — aucun canal de dégâts-subis-par-attaquant-spécifique
        // n'existe côté moteur (seule une réduction de Défense générique, affectant
        // tous les attaquants, est disponible) ; approximé par un débuff de Défense.
        await UpsertSkillAsync("canon.skill.constat-sec", "Constat sec",
            "Elle subit +15% dégâts de l'Écho.",
            "Debuff", "SingleEnemy", "Debuff", mana: 8, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -15, TicksPerTurn * 3, Stat: "Defense", MagnitudeIsPercentOfBaseStat: true) },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.montee", "Montée",
            "Ça monte. Personne ne calmera rien.",
            "Buff", "Self", "Buff", mana: 6, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, 5, TicksPerTurn * 3, Stat: "AttackPower") },
            category: "Physical");

        await UpsertSkillAsync("canon.skill.explosion", "Explosion",
            "Ça devait éclater.",
            "Damage", "AllEnemies", "Damage", mana: 16, power: 20, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.frisson", "Frisson",
            "Quelque chose a bougé derrière vous.",
            "Damage", "SingleEnemy", "Damage", mana: 6, power: 9, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -12, TicksPerTurn * 2, Stat: "AtbTempoModifier") },
            category: "Magic");

        // "Porte fermée" : la doc ajoute un verrou de rang — sans objet côté moteur
        // (pas de système de rang) ; seul le débuff de Focus est câblé.
        await UpsertSkillAsync("canon.skill.porte-fermee", "Porte fermée",
            "Il n'y a pas de sortie.",
            "Debuff", "SingleEnemy", "Debuff", mana: 10, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -3, TicksPerTurn * 2, Stat: "Focus") },
            category: "Magic");

        // "Saccade" : la doc décrit un changement de rang + 25% esquive — sans objet
        // côté moteur ; approximé par une garde instantanée.
        await UpsertSkillAsync("canon.skill.saccade", "Saccade",
            "Là où vous regardez, il n'est déjà plus.",
            "Buff", "Self", "Guard", mana: 4, power: 6, cancellationToken,
            category: "Physical");

        await UpsertSkillAsync("canon.skill.poids", "Poids",
            "Tout devient un peu plus loin.",
            "Debuff", "SingleEnemy", "Debuff", mana: 8, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -15, TicksPerTurn * 3, Stat: "Speed", MagnitudeIsPercentOfBaseStat: true) },
            category: "Magic");

        // "Constat tardif" : la doc décrit un ciblage sur "la cible qui a agi il y a
        // le plus longtemps" — aucun suivi d'ordre d'action n'existe côté moteur ;
        // ciblage laissé à l'IA (cible la plus faible), simplification à
        // assumer/affiner plus tard.
        await UpsertSkillAsync("canon.skill.constat-tardif", "Constat tardif",
            "Toujours en retard, toujours exact.",
            "Damage", "SingleEnemy", "Damage", mana: 10, power: 16, cancellationToken,
            category: "Magic");

        await UpsertSkillAsync("canon.skill.silence-partage", "Silence partagé",
            "Le seul répit qu'il connaisse. Il le partage.",
            "Heal", "AllAllies", "Heal", mana: 12, power: 6, cancellationToken,
            category: "Magic", basePowerIsPercentOfMaxVitality: true);

        await UpsertEnemyAsync(
            "canon.enemy.echo-colere", "Écho de Colère",
            "Une déchirure rouge sombre dans l'air, en forme de geste interrompu — un poing levé qui n'est jamais retombé. Elle vibre d'une chaleur sèche et cherche, en permanence, quelque chose qui mérite d'éclater. « Ça n'a plus personne à défendre. Ça frappe quand même. »",
            "Bruiser", family, "Uncommon", "Bruiser", isElite: false,
            depthMin: 3, depthMax: 10, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "rupture", "echos-d-emotions", "dissonance" },
            skillKeys: new[] { "canon.skill.eclat-echo-colere", "canon.skill.constat-sec", "canon.skill.montee", "canon.skill.explosion" },
            vitality: 60, attack: 13, defense: 5, guard: 0, speed: 9, focus: 3,
            cancellationToken,
            magicAttack: 9, magicDefense: 6, initiative: 9, mana: 14, menace: 5,
            rarity: "Uncommon", registre: "Rupture");

        await UpsertEnemyAsync(
            "canon.enemy.echo-peur", "Écho de Peur",
            "Un frémissement pâle qui n'est jamais tout à fait là où on le regarde. Il se déplace par saccades, longe les murs, et son contact donne l'exacte sensation d'une porte qu'on trouve fermée dans le noir. « Il guette une sortie qui n'existe plus. Vous êtes entre lui et elle. »",
            "Disruptor", family, "Uncommon", "Disruptor", isElite: false,
            depthMin: 3, depthMax: 10, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "effroi", "echos-d-emotions", "dissonance" },
            skillKeys: new[] { "canon.skill.frisson", "canon.skill.porte-fermee", "canon.skill.saccade", "canon.skill.nevrose" },
            vitality: 42, attack: 6, defense: 4, guard: 0, speed: 14, focus: 6,
            cancellationToken,
            magicAttack: 10, magicDefense: 8, initiative: 14, mana: 18, menace: 4,
            rarity: "Uncommon", registre: "Effroi",
            boundRoomKeys: new[] { "room.feelings", "room.falaise", "room.couloirs" });

        await UpsertEnemyAsync(
            "canon.enemy.echo-tristesse", "Écho de Tristesse",
            "Une lenteur visible — l'air lui-même semble plus épais autour de lui. Il a vaguement la forme d'une personne assise, même quand il se déplace. Ceux qui le traversent se souviennent soudain de tout ce qu'ils n'ont pas dit à temps. « Il ne pleure pas. Il constate, longtemps après tout le monde. »",
            "Support", family, "Uncommon", "Support", isElite: false,
            depthMin: 3, depthMax: 10, riskMin: 2, riskMax: 4,
            roomTypes: new[] { "Memory" },
            tags: new[] { "bestiaire", "melancolie", "echos-d-emotions", "dissonance", "dot" },
            skillKeys: new[] { "canon.skill.poids", "canon.skill.sursaut-memoriel", "canon.skill.constat-tardif", "canon.skill.silence-partage" },
            vitality: 80, attack: 7, defense: 9, guard: 0, speed: 3, focus: 6,
            cancellationToken,
            magicAttack: 11, magicDefense: 11, initiative: 2, mana: 22, menace: 5,
            rarity: "Uncommon", registre: "Melancolie",
            boundRoomKeys: new[] { "room.feelings", "room.room08", "room.hopital" });
    }

    private async Task SeedBestiaireImperatriceDeLaFalaiseAsync(CancellationToken cancellationToken)
    {
        // "L'Impératrice de la Falaise" — mini-boss unique par run, gate-keeper
        // optionnel des enfers. N'appartient à aucune famille du Bestiaire (registre
        // Effroi/Silence, à part). Le plafond "unique par run" documenté n'est pas
        // appliqué mécaniquement (aucun compteur d'apparition par run côté moteur) ;
        // sa rareté (Legendary) et son menace élevé la rendent naturellement rare via
        // la sélection de rencontre existante.

        await UpsertSkillAsync("canon.skill.maree-montante", "Marée montante",
            "La falaise se rétrécit.",
            "Debuff", "AllEnemies", "Debuff", mana: 14, power: 0, cancellationToken,
            effects: new[] { new SkillEffectSpec("StatModifier", null, -10, TicksPerTurn * 2, Stat: "Speed", MagnitudeIsPercentOfBaseStat: true) },
            category: "Magic");

        await UpsertSkillAsync("canon.skill.lame-de-fond", "Lame de fond",
            "La mer achève ce qu'elle a commencé.",
            "Damage", "SingleEnemy", "Damage", mana: 18, power: 26, cancellationToken,
            category: "Physical");

        // Variante "+50% sur cible sous 2+ DoT" de Lame de fond : le moteur ne
        // permet pas de moduler la puissance d'un sort selon l'état de la cible au
        // moment du calcul de dégâts, donc ce bonus est authoré comme un second sort
        // à puissance fixe majorée, sélectionné par l'IA quand la condition est
        // remplie (voir ImperatriceBossBehavior), plutôt que comme un multiplicateur
        // dynamique.
        await UpsertSkillAsync("canon.skill.lame-de-fond-renforcee", "Lame de fond (renforcée)",
            "La mer achève ce qu'elle a commencé — plus fort encore.",
            "Damage", "SingleEnemy", "Damage", mana: 18, power: 39, cancellationToken,
            category: "Physical");

        await UpsertEnemyAsync(
            "canon.enemy.imperatrice", "L'Impératrice",
            "Une silhouette féminine démesurée émergeant à mi-corps de la mer violacée, couronnée d'une structure qui évoque à la fois un diadème et une cage thoracique renversée. Sa robe est la mer — littéralement : les vagues sont son ourlet, et la marée suit ses humeurs. « Malheureux sont ceux qui croiseront l'impératrice dans ce lieu. »",
            "Bruiser", "Imperatrice de la Falaise", "Legendary", "Bruiser", isElite: true,
            depthMin: 2, depthMax: 9, riskMin: 3, riskMax: 5,
            roomTypes: new[] { "Silence" },
            tags: new[] { "bestiaire", "effroi", "silence", "mini-boss", "unique" },
            skillKeys: new[] { "canon.skill.deluge-du-styx", "canon.skill.symphonie-des-enfers", "canon.skill.maree-montante", "canon.skill.lame-de-fond", "canon.skill.lame-de-fond-renforcee" },
            vitality: 240, attack: 13, defense: 11, guard: 0, speed: 7, focus: 9,
            cancellationToken,
            magicAttack: 17, magicDefense: 14, initiative: 8, mana: 40, menace: 10,
            rarity: "Legendary", registre: "Effroi",
            boundRoomKeys: new[] { "room.falaise" });
    }

    private async Task UpsertSkillAsync(
    string key, string name, string description,
    string skillType, string targeting, string effectType,
    int mana, int power, CancellationToken cancellationToken,
    IReadOnlyList<SkillEffectSpec>? effects = null,
    string category = "Physical",
    bool basePowerIsPercentOfMaxVitality = false)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var effectsJson = JsonSerializer.Serialize(effects ?? [], J);
        var existing = await _ctx.SkillDefinitions.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.SkillDefinitions.Add(new SkillDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name,
                DisplayName = name,
                Description = description,
                NarrativeText = description,
                Version = version,
                Status = "Active",
                SkillType = skillType,
                TargetingType = targeting,
                TargetingMode = targeting,
                EffectType = effectType,
                Category = category,
                CostType = mana > 0 ? "Mana" : "None",
                ManaCost = mana,
                ChargeCost = 0,
                BasePower = power,
                BasePowerIsPercentOfMaxVitality = basePowerIsPercentOfMaxVitality,
                Power = power,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                EffectsJson = effectsJson,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }
        existing.Name = name; existing.DisplayName = name;
        existing.Description = description; existing.NarrativeText = description;
        existing.Version = version; existing.Status = "Active";
        existing.SkillType = skillType; existing.TargetingType = targeting; existing.TargetingMode = targeting;
        existing.EffectType = effectType; existing.Category = category; existing.CostType = mana > 0 ? "Mana" : "None";
        existing.ManaCost = mana; existing.BasePower = power; existing.Power = power;
        existing.BasePowerIsPercentOfMaxVitality = basePowerIsPercentOfMaxVitality;
        existing.EffectsJson = effectsJson;
        existing.UpdatedAtUtc = now;
    }

    // ── OBJETS CANON ──────────────────────────────────────────────────────────
    private async Task SeedCanonItemsAsync(CancellationToken cancellationToken)
    {
        // key, name, desc, category, itemType, rarity, lifecycle, usableInCombat, effectValue
        // TODO(utilisateur) : les objets ci-dessous avec Duration="Permanent" sont désormais
        // automatiquement éligibles au sac permanent (SFD "Équipement et sac permanent" § 4) —
        // mais aucun n'a d'effet d'équipement assigné (equipmentEffects reste vide). Ne rien
        // inventer ; compléter au cas par cas une fois le contenu (bonus, sorts, affinités) reçu.
        await UpsertItemAsync("canon.item.tome-38", "Le Tome 38",
            "« L'épopée du Silence ». Les notes du 38ᵉ écho, reliées dans une peau humaine. Celui qui le lit n'est jamais tout à fait seul.",
            "Relic", "Lore", "Unique", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.carnet-pomenian", "Le carnet de Pomenian",
            "Des observations méthodiques, une écriture qui se dégrade page après page. Des vérités que l'auteur aurait dû taire.",
            "Relic", "Lore", "Rare", "Permanent", false, 0, cancellationToken);

        // Le carnet du premier architecte (Thomas, objet rare) : un objet "lisible" —
        // le joueur peut le consulter page par page (voir BookReader côté frontend).
        // TODO(utilisateur) : contenu réel à venir ; une seule page placeholder pour
        // l'instant, le mécanisme supporte déjà plusieurs pages (un texte long sera
        // simplement découpé en plusieurs entrées de ce tableau).
        await UpsertItemAsync("canon.item.carnet-premier-architecte", "Le carnet du premier architecte",
            "Les pages sont usées, annotées d'une main assurée. On y devine, plus qu'on y lit, le Palais tel que son premier architecte l'a vécu.",
            "Relic", "Lore", "Rare", "Permanent", false, 0, cancellationToken,
            readablePages: new[] { "PLACEHOLDER HISTOIRE 01" });

        await UpsertItemAsync("canon.item.masque-bec-oiseau", "Masque à bec d'oiseau",
            "Masque vénitien rempli d'herbes. Contre la peste — et contre l'air vicié des couloirs.",
            "Equipment", "Protection", "Uncommon", "RunOnly", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.oeil-visionnaire", "L'Œil du Visionnaire",
            "Sceau lituique, pupille en amande violacée et jaune. Il protège ceux qui croient — et marque ceux qui doutent.",
            "Relic", "Seal", "Epic", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.khamsa", "La Khamsa",
            "La main contre le mauvais œil. Tant qu'on ne la franchit pas, la malédiction glisse.",
            "Relic", "Ward", "Rare", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.ouroboros", "L'Ouroboros",
            "Le serpent qui se mord la queue. Un masque à huit yeux. Le commencement est la fin est le commencement.",
            "Relic", "Symbol", "Epic", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.amethyste", "Améthyste papale",
            "Gemme violette sertie d'or, arrachée aux ornements du clergé. Elle apaise — ou achète.",
            "Material", "Gem", "Uncommon", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.datura", "Datura stramonium",
            "Cinq drachmes. La fièvre, les visions, la porte entrebâillée vers l'autre côté. Un pas de trop et l'on ne revient pas.",
            "Consumable", "Potion", "Rare", "RunOnly", true, 18, cancellationToken);

        await UpsertItemAsync("canon.item.lanterne", "Lanterne à huile",
            "Seules les chaumières éclairées ne furent pas touchées. La lumière est un abri.",
            "Consumable", "Light", "Common", "RunOnly", true, 0, cancellationToken);

        await UpsertItemAsync("canon.item.fil-ariane", "Le fil d'Ariane",
            "Fait d'espoir et d'amour, visible seulement par-delà la barrière de glace. Les échos d'avant l'ont tendu pour toi.",
            "Key", "Thread", "Legendary", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.boite-homoncule", "La boîte de l'Homoncule",
            "Un coffret et sa clé. Ce qui dort dedans n'attend que d'être nommé.",
            // TODO(utilisateur): équipement légendaire — transforme le "type" du joueur en on ne sait
            // encore quoi. EquipmentEffects volontairement vide tant que la mécanique cible n'est pas
            // définie (GrantAffinity ne convient pas : il ne s'agit pas d'une affinité émotionnelle).
            "Equipment", "Transmutation", "Legendary", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.fiole-cristal", "Fiole de cristal",
            "Un verre trop pur pour être humain. Elle garde ce qu'on y verse, quel qu'en soit le nom.",
            "Relic", "Container", "Rare", "Permanent", false, 0, cancellationToken,
            isContainer: true, containerCapacity: 1);

        await UpsertItemAsync("canon.item.flamme-seraphine", "La Flamme Séraphine",
            "Une flamme à recueillir, jamais à posséder. Elle accorde le seul feu qui fasse hurler l'Homoncule.",
            "Relic", "Flame", "Legendary", "Permanent", true, 0, cancellationToken);

        // ── Butin canon (loot d'ennemis) — consommables/matériaux ordinaires,
        //    distincts des reliques ci-dessus qui restent uniques/de quête.
        await UpsertItemAsync("canon.item.cendre-benite", "Cendre bénite",
            "Un peu de cendre récupérée d'un cierge d'abbaye. Elle protège, faiblement, ceux qui la portent.",
            "Consumable", "Ward", "Common", "RunOnly", false, 5, cancellationToken);

        await UpsertItemAsync("canon.item.dent-vorace", "Dent vorace",
            "Arrachée à une créature affamée. Encore chaude, encore vivante d'une certaine manière.",
            "Material", "Trophy", "Uncommon", "RunOnly", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.filament-de-brume", "Filament de brume",
            "Un fragment de brouillard qui refuse de se dissiper. Utile pour brouiller à son tour.",
            "Consumable", "Utility", "Uncommon", "RunOnly", true, 8, cancellationToken);

        await UpsertItemAsync("canon.item.larme-de-racine", "Larme de racine",
            "Une sève amère, presque une larme. Elle apaise la chair comme elle apaisait autrefois la mémoire.",
            "Consumable", "Heal", "Common", "RunOnly", true, 12, cancellationToken);

        await UpsertItemAsync("canon.item.sel-alchimique", "Sel alchimique",
            "Un sel instable, encore tiède de la cornue. Les alchimistes du Palais s'en disputent le dernier grain.",
            "Material", "Reagent", "Uncommon", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.poussiere-de-tombe", "Poussière de tombe",
            "Une poussière banale, ramassée là où l'on ne devrait pas fouiller.",
            "Consumable", "Narrative", "Common", "RunOnly", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.onguent-anxiete", "Onguent d'apaisement",
            "Un baume lourd, presque suffocant, qui étouffe la panique avant qu'elle n'étouffe toi.",
            "Consumable", "Heal", "Rare", "RunOnly", true, 20, cancellationToken);

        await UpsertItemAsync("canon.item.eclat-de-vipere", "Éclat de vipère",
            "Une écaille tombée de l'Impératrice. Elle garde, longtemps après, un éclat venimeux.",
            "Material", "Trophy", "Rare", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.parchemin-cardinal", "Parchemin scellé",
            "Un décret inachevé, encore scellé de cire rouge. Son contenu n'a plus d'importance — le sceau, si.",
            "Consumable", "Lore", "Uncommon", "RunOnly", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.ecaille-himlit", "Écaille d'Him'Lit",
            "Une écaille arrachée au maître des lieux. Elle pèse plus lourd qu'elle ne devrait.",
            "Material", "Trophy", "Epic", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.craie-creatrice", "Craie créatrice",
            "Un bâton de craie qui n'en finit pas de s'user. Ce qu'elle dessine résiste, un peu, à l'oubli.",
            "Equipment", "Accessory", "Rare", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[] { new ItemEquipmentEffect(
                ItemEquipmentEffectKind.DamageReductionByType, Amount: 15, AffinityRegister: EmotionalRegister.Memoire) });

        await UpsertItemAsync("canon.item.main-de-khasma", "Main de Khasma",
            "Une main de bronze gravée de symboles qu'aucun vivant ne sait plus lire. Elle referme ce qui devrait pourrir.",
            "Equipment", "Accessory", "Legendary", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[]
            {
                new ItemEquipmentEffect(ItemEquipmentEffectKind.DotDurationReduction, Amount: 25),
                new ItemEquipmentEffect(ItemEquipmentEffectKind.DotDamageReduction, Amount: 15)
            });

        await UpsertItemAsync("canon.item.lunettes-erudit", "Lunettes d'érudit",
            "Des verres taillés pour lire l'invisible. Ce qu'elles regardent, elles ne le manquent plus.",
            "Equipment", "Accessory", "Rare", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[] { new ItemEquipmentEffect(ItemEquipmentEffectKind.HitChanceBonus, Amount: 10) });

        await UpsertItemAsync("canon.item.bague-du-courage", "Bague du courage",
            "Un anneau simple, sans ornement. Ceux qui le portent avancent, tout simplement.",
            "Equipment", "Accessory", "Epic", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[]
            {
                new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "Speed", Amount: 10),
                new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "AttackPower", Amount: 10)
            });

        await UpsertItemAsync("canon.item.potion-de-vie", "Potion de vie",
            "Un liquide rouge sombre, épais comme du sang. Il referme ce que le Palais a ouvert.",
            "Consumable", "Potion", "Common", "RunOnly", true, 25, cancellationToken,
            effectRunType: "Heal");

        await UpsertItemAsync("canon.item.tasse-de-the", "Tasse de thé",
            "Toujours chaude, quelle que soit l'heure — comme si le Majordome savait, avant vous, que vous en auriez besoin. Redonne 35% des PV et des PP maximum.",
            "Consumable", "Potion", "Rare", "RunOnly", true, 35, cancellationToken,
            effectRunType: "HealAndManaRestorePercent");

        await UpsertItemAsync("canon.item.tasse-du-majordome", "La tasse du majordome",
            "Sa propre tasse, qu'il ne prête jamais — sauf, une fois, à vous. La porter, c'est un peu apprendre de lui l'art de veiller sur autrui. Augmente les effets de soin de 15%.",
            "Equipment", "Accessory", "Legendary", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[] { new ItemEquipmentEffect(ItemEquipmentEffectKind.HealingBonusPercent, Amount: 15) });

        await UpsertItemAsync("canon.item.reve-erina", "Rêve d'Erina",
            "Un fragment de ce qu'elle imagine derrière chaque porte fermée. Tant qu'on le garde sur soi, on avance plus vite — comme elle.",
            "Relic", "Memento", "Rare", "RunOnly", false, 5, cancellationToken,
            effectRunType: "TeamSpeedBonus");

        await UpsertItemAsync("canon.item.monocle-pomenian", "Le monocle de Pomenian",
            "Une lentille gravée de formules alchimiques anciennes — celles-là mêmes que Pomenian refuse de considérer comme autre chose que des curiosités d'érudit. Quiconque le chausse voit, malgré lui, un peu plus loin que les livres.",
            "Equipment", "Accessory", "Epic", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[] { new ItemEquipmentEffect(ItemEquipmentEffectKind.MagicDamageBonusPercent, Amount: 10) });

        await UpsertItemAsync("canon.item.marteau-de-forge", "Marteau de forge",
            "L'outil du Forgeron — celui qui a donné forme à tout ce qui marche dans le Palais. Il pèse plus qu'il ne devrait.",
            "Equipment", "Accessory", "Rare", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[] { new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "AttackPower", Amount: 10) });

        // Aucun effet d'équipement classique : sa mécanique (journalisation automatique des
        // événements de run) est branchée par clé directement côté game-engine, pas via
        // ItemEquipmentEffectKind — voir SeedForgeronAsync/SeedEmotionsAsync et StartRunCommandHandler.
        await UpsertItemAsync("canon.item.carnet-de-bord", "Carnet de bord",
            "Un carnet vierge qui ne le reste jamais longtemps. Une fois en ta possession, il consigne de lui-même, run après run, tout ce que tu traverses.",
            "Equipment", "Accessory", "Rare", "Permanent", false, 0, cancellationToken);

        // Aucun effet d'équipement classique : sa mécanique (retirer une loi active du Palais,
        // une fois toutes les 10 rooms) est branchée par clé directement côté game-engine, pas
        // via ItemEquipmentEffectKind — voir SeedErikaAsync et StartRunCommandHandler.
        await UpsertItemAsync("canon.item.deni-permanent", "Déni permanent",
            "Une clause qu'Erika a arrachée aux marges du Palais, avant même que quiconque songe à en dresser les lois. La brandir, c'est faire vaciller une règle — le temps de la faire taire.",
            "Equipment", "Accessory", "Legendary", "Permanent", false, 0, cancellationToken);

        // Aucun effet d'équipement classique : le bonus de gain de réputation (+10%) est
        // branché par clé directement côté game-engine, pas via ItemEquipmentEffectKind —
        // voir SeedMinaAsync et StartRunCommandHandler.ReputationBoostItemKey.
        await UpsertItemAsync("canon.item.peluche-mina", "Peluche de Mina",
            "Usée, cousue de fil grossier — la seule chose que Mina possédait avant de te la confier. La garder sur soi, c'est porter un peu de sa confiance.",
            "Relic", "Keepsake", "Rare", "Permanent", false, 0, cancellationToken);

        // Aucun effet d'équipement classique : rencontres Him'Lit +50% (génération de
        // room) + bundle de statut de combat "Ce n'était pas pour vous..." sont branchés
        // par clé directement côté game-engine, pas via ItemEquipmentEffectKind — voir
        // SeedMinaAsync, StartRunCommandHandler.HimLitProtectionItemKey, DeterministicRunGenerator
        // et CombatFactory.ApplyHimLitProtection.
        await UpsertItemAsync("canon.item.protection-himlit", "Protection de Him'Lit",
            "Une marque que Him'Lit lui-même a posée sur Mina, et qu'elle a un jour posée sur toi. Elle attire son regard plus souvent qu'elle ne devrait — et pèse, un peu, sur ce que tu portes en combat.",
            "Relic", "Ward", "Legendary", "Permanent", false, 0, cancellationToken);

        // Aucun effet d'équipement classique : sa mécanique (restaurer 50% des PV max
        // d'une cible, une fois par Room) est branchée par clé directement côté
        // game-engine, pas via ItemEquipmentEffectKind — voir SeedJohnAsync,
        // StartRunCommandHandler.CaliceInfiniItemKey et Run.UseCaliceInfini.
        await UpsertItemAsync("canon.item.calice-infini", "Calice infini",
            "Un calice que John a gardé de ses années de pillage, avant que le Palais ne l'envoie digérer par Him'Lit. Il ne se vide jamais tout à fait — une gorgée suffit à refaire ce qui a été perdu.",
            "Equipment", "Accessory", "Legendary", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.pierre-antique", "Pierre antique",
            "Une pierre arrachée aux fondations d'un temple oublié — plus dure que tout ce que le Palais a jamais bâti.",
            "Equipment", "Accessory", "Rare", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[] { new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "Defense", Amount: 10) });

        await UpsertItemAsync("canon.item.doudou-ethan", "Doudou de Ethan",
            "Un doudou usé, cousu à la main. Iris ne le donne qu'à ceux qui ont mérité, un peu, sa confiance.",
            "Equipment", "Accessory", "Rare", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[] { new ItemEquipmentEffect(ItemEquipmentEffectKind.CriticalChanceBonusPercent, Amount: 5) });

        // +20% de la garde de départ du combat (0 reste 0 ; 100 devient 120) — voir
        // StartRunCommandHandler.guardBonusPercent / CombatFactory.guardBonus.
        await UpsertItemAsync("canon.item.bague-iris", "Bague de Iris",
            "Un anneau qu'Iris a un jour retiré de son propre doigt, sans un mot, pour le lui donner. Il ne dit rien non plus, en l'acceptant.",
            "Equipment", "Accessory", "Legendary", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[] { new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "Guard", Amount: 20) });

        await UpsertItemAsync("canon.item.marque-de-creation", "Marque de création",
            "Une empreinte laissée par l'Architecte lui-même — la trace d'une proportion qu'il juge, enfin, correcte.",
            "Equipment", "Accessory", "Rare", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[]
            {
                new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "MaxVitality", Amount: 5),
                new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "AttackPower", Amount: 5),
                new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "Defense", Amount: 5),
                new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "Speed", Amount: 5),
                new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonusPercent, StatKind: "Focus", Amount: 5)
            });

        await UpsertItemAsync("canon.item.plume-ecrivain", "Plume d'écrivain",
            "Une plume usée jusqu'à la corde, trempée dans une encre qui ne sèche jamais tout à fait.",
            "Equipment", "Accessory", "Rare", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[]
            {
                new ItemEquipmentEffect(ItemEquipmentEffectKind.DotDamageBonusPercent, Amount: 5)
            });
    }

    private async Task UpsertItemAsync(
        string key, string name, string description,
        string category, string itemType, string rarity, string durability,
        bool usableInCombat, int effectValue, CancellationToken cancellationToken,
        IReadOnlyList<ItemEquipmentEffect>? equipmentEffects = null,
        bool isContainer = false, int? containerCapacity = null, bool isLiquid = false,
        string? effectRunType = null, IReadOnlyList<string>? readablePages = null)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var lifecycle = durability == "Permanent" ? "PersistentMeta" : "RuntimeRunOnly";
        var duration = durability == "Permanent" ? "Permanent" : "RunOnly";
        var equipmentEffectsJson = JsonSerializer.Serialize(equipmentEffects ?? [], J);
        var readablePagesJson = JsonSerializer.Serialize(readablePages ?? [], J);
        var existing = await _ctx.ItemDefinitions.FirstOrDefaultAsync(i => i.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.ItemDefinitions.Add(new ItemDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name,
                DisplayName = name,
                Description = description,
                NarrativeText = description,
                Version = version,
                Status = "Active",
                Category = category,
                ItemType = itemType,
                Rarity = rarity,
                UsageMode = usableInCombat ? "UseInCombat" : "NotUsable",
                Lifecycle = lifecycle,
                StackPolicy = "Additive",
                MaxStack = 1,
                IsUsableInCombat = usableInCombat,
                IsUsableOutsideCombat = false,
                Duration = duration,
                EffectValue = effectValue,
                EffectRunType = effectRunType,
                EquipmentEffectsJson = equipmentEffectsJson,
                IsContainer = isContainer,
                ContainerCapacity = containerCapacity,
                IsLiquid = isLiquid,
                ReadablePagesJson = readablePagesJson,
                Price = 0,
                BaseWeight = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }
        existing.Name = name; existing.DisplayName = name;
        existing.Description = description; existing.NarrativeText = description;
        existing.Version = version; existing.Status = "Active";
        existing.Category = category; existing.ItemType = itemType; existing.Rarity = rarity;
        existing.UsageMode = usableInCombat ? "UseInCombat" : "NotUsable";
        existing.Lifecycle = lifecycle; existing.Duration = duration;
        existing.IsUsableInCombat = usableInCombat; existing.EffectValue = effectValue;
        existing.EffectRunType = effectRunType;
        existing.EquipmentEffectsJson = equipmentEffectsJson;
        existing.IsContainer = isContainer;
        existing.ContainerCapacity = containerCapacity;
        existing.IsLiquid = isLiquid;
        existing.ReadablePagesJson = readablePagesJson;
        existing.UpdatedAtUtc = now;
    }
    // ── MALÉDICTIONS CANON ────────────────────────────────────────────────────
    private async Task SeedCanonCursesAsync(CancellationToken cancellationToken)
    {
        // key, name, desc, severity, duration, trigger, minDepth
        await UpsertCurseAsync("canon.curse.peste-paiens", "La peste des païens",
            "« La peste ne frappe que les païens. » Une préférence trop logique pour être naturelle. Une entité est à l'œuvre.",
            severity: 3, duration: "UntilRunEnds", trigger: "OnApplied", minDepth: 1, cancellationToken);

        await UpsertCurseAsync("canon.curse.mauvais-oeil", "Le mauvais œil",
            "Tu as franchi la Khamsa. Le regard violacé t'a trouvé. Désormais, la chance se détourne.",
            severity: 2, duration: "UntilRunEnds", trigger: "OnApplied", minDepth: 1, cancellationToken);

        await UpsertCurseAsync("canon.curse.don-empoisonne", "Le don empoisonné",
            "La femme aux yeux bleus maudits t'a offert un présent. Tout cadeau de la Vipère porte son venin.",
            severity: 3, duration: "UntilRunEnds", trigger: "NextCombatStarted", minDepth: 2, cancellationToken);

        await UpsertCurseAsync("canon.curse.morsure-flamme-froide", "La morsure de la flamme froide",
            "Le givre te tient. Il ne brûle pas la peau mais la chair, lentement, à chaque pas.",
            severity: 2, duration: "NextCombatOnly", trigger: "NextCombatStarted", minDepth: 1, cancellationToken);
    }

    private async Task UpsertCurseAsync(
        string key, string name, string description,
        int severity, string duration, string trigger, int minDepth,
        CancellationToken cancellationToken)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var existing = await _ctx.CurseDefinitions.FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.CurseDefinitions.Add(new CurseDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = name,
                Description = description,
                NarrativeText = description,
                Severity = severity,
                Duration = duration,
                Trigger = trigger,
                EffectSetId = null,
                BaseWeight = 1,
                MinDepth = minDepth,
                SelectionGroup = "curse.canon",
                Version = version,
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }
        existing.DisplayName = name; existing.Description = description; existing.NarrativeText = description;
        existing.Severity = severity; existing.Duration = duration; existing.Trigger = trigger;
        existing.MinDepth = minDepth; existing.Version = version; existing.Status = "Active";
        existing.UpdatedAtUtc = now;
    }
    // ── LOIS DU PALAIS CANON (arrêtés papaux) ─────────────────────────────────
    private async Task SeedCanonLawsAsync(CancellationToken cancellationToken)
    {
        // key, name, desc, severity, visibility, priority, impactDomains, trigger
        await UpsertLawAsync("canon.law.arrete-153-2", "Arrêté papal n°153-2",
            "Interdiction formelle de tout véhicule étranger dans l'enceinte sainte. L'étranger marche, ou ne marche pas.",
            severity: 1, visibility: "Visible", priority: 1,
            impactDomains: new[] { "Events", "Generation" }, trigger: "OnApplied", cancellationToken);

        await UpsertLawAsync("canon.law.interdiction-construire", "Interdiction de construire",
            "Nul ne bâtit sans la bénédiction du clergé. Ce qui s'élève sans permission s'effondre.",
            severity: 1, visibility: "Visible", priority: 2,
            impactDomains: new[] { "Generation" }, trigger: "OnApplied", cancellationToken);

        await UpsertLawAsync("canon.law.creation-vie-heresie", "Création de vie : hérésie",
            "Donner la vie est le privilège de Dieu seul. Quiconque transmute le souffle sera décapité.",
            severity: 3, visibility: "Visible", priority: 5,
            impactDomains: new[] { "Narrative", "Combat" }, trigger: "OnTransgression", cancellationToken);

        await UpsertLawAsync("canon.law.prieres-impies-nocturnes", "Prières impies nocturnes",
            "À la tombée de la nuit, les prières montent de l'abbaye. Ce qui se nourrit de la voix s'en trouve renforcé.",
            severity: 2, visibility: "PartiallyVisible", priority: 3,
            impactDomains: new[] { "Combat" }, trigger: "OnCombatStarted", cancellationToken);

        await UpsertLawAsync("canon.law.reflets-de-lune", "Le poids des reflets de Lune",
            "Him'Lit n'est pas encore là. Mais la Lune le précède, et son influence croît à chaque palier franchi.",
            severity: 3, visibility: "Hidden", priority: 9,
            impactDomains: new[] { "HimLit", "Rewards" }, trigger: "OnDepthIncreased", cancellationToken);
    }

    private async Task AttachCanonLawEffectsAsync(CancellationToken cancellationToken)
    {
        // lawKey, effectType (cf. EffectType côté game-engine), value, duration, condition
        await UpsertLawEffectAsync("canon.law.reflets-de-lune", "ModifyDifficultyMultiplier", 0.30m, "UntilRunEnds", null, cancellationToken);
        await UpsertLawEffectAsync("canon.law.creation-vie-heresie", "ModifyDifficultyMultiplier", 0.20m, "UntilRunEnds", null, cancellationToken);
        await UpsertLawEffectAsync("canon.law.prieres-impies-nocturnes", "ModifyDifficultyMultiplier", 0.15m, "UntilRunEnds", null, cancellationToken);
        await UpsertLawEffectAsync("canon.law.interdiction-construire", "ModifyRewardPowerMultiplier", -0.15m, "UntilRunEnds", null, cancellationToken);
        await UpsertLawEffectAsync("canon.law.arrete-153-2", "ModifySpeed", -0.20m, "UntilRunEnds", null, cancellationToken);
    }

    private async Task UpsertLawEffectAsync(
        string lawKey, string effectType, decimal value, string duration, string? condition,
        CancellationToken cancellationToken)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var effectSetKey = $"effect.{lawKey}";

        var law = await _ctx.PalaceLawDefinitions.FirstOrDefaultAsync(l => l.Key == lawKey, cancellationToken);
        if (law is null) return;

        var effectSet = await _ctx.EffectSets
            .FirstOrDefaultAsync(e => e.Key == effectSetKey, cancellationToken);
        if (effectSet is null)
        {
            effectSet = new EffectSetEntity
            {
                Id = Guid.NewGuid(),
                Key = effectSetKey,
                DisplayName = law.DisplayName,
                Description = law.Description,
                Version = version,
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            effectSet.Effects.Add(new EffectDefinitionEntity
            {
                Id = Guid.NewGuid(),
                EffectSetId = effectSet.Id,
                EffectType = effectType,
                TargetScope = "Run",
                Value = condition is null ? value : null,
                ValueMode = "Flat",
                Duration = duration,
                StackPolicy = "Additive",
                Condition = condition,
                Order = 0
            });
            _ctx.EffectSets.Add(effectSet);
        }

        law.EffectSetId = effectSet.Id;
        law.UpdatedAtUtc = now;
    }

    private async Task UpsertLawAsync(
        string key, string name, string description,
        int severity, string visibility, int priority,
        string[] impactDomains, string trigger, CancellationToken cancellationToken)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var domainsJson = JsonSerializer.Serialize(impactDomains);
        var existing = await _ctx.PalaceLawDefinitions.FirstOrDefaultAsync(l => l.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.PalaceLawDefinitions.Add(new PalaceLawDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name,
                DisplayName = name,
                Description = description,
                NarrativeText = description,
                Version = version,
                Status = "Active",
                Scope = "Run",
                Duration = "UntilRunEnds",
                Trigger = trigger,
                Severity = severity,
                Visibility = visibility,
                Priority = priority,
                EffectSetId = null,
                BaseWeight = 1,
                SelectionGroup = "law.canon",
                ImpactDomainsJson = domainsJson,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }
        existing.Name = name; existing.DisplayName = name;
        existing.Description = description; existing.NarrativeText = description;
        existing.Version = version; existing.Status = "Active";
        existing.Trigger = trigger; existing.Severity = severity;
        existing.Visibility = visibility; existing.Priority = priority;
        existing.ImpactDomainsJson = domainsJson; existing.UpdatedAtUtc = now;
    }

    // ── COMPENDIUM DES LOIS DU PALAIS (40 lois, Phase 4) ─────────────────────
    //
    // Chapitre VIII — Lois majeures & paradoxales. All 5 laws now have full mechanical
    // backing (Reflet/Sablier Renversé/Dévoration/Treizième Coup from Phase 3, Destinée
    // added afterward reusing the existing "Une destinée cruelle" canon skill bundle).
    //
    // Promulgation rules NOT enforced by the engine yet (documented gap, same convention
    // as the Phase 3 "document but simplify" pattern) — both would need new per-run
    // tracking state: "jamais deux fois par run" (Reflet, Destinée), "poids doublé si
    // Ethan a été rencontré cette run" (Dévoration). Majeure exclusivity itself
    // ("jamais deux lois majeures actives simultanément") IS enforced, by IsMajeure.
    private async Task SeedLoisMajeuresAsync(CancellationToken cancellationToken)
    {
        await UpsertCompendiumLawAsync(
            key: "law.reflet",
            name: "Loi du Reflet",
            narrativeText: "Article LXVI — Le Palais vous a assez regardés. Voyez ce qu'il voit.",
            description: "Le prochain combat remplace les ennemis prévus par des reflets de "
                + "l'équipe : mêmes sorts, mêmes accessoires, 60% des statistiques. Les reflets "
                + "connaissent vos habitudes — leur IA copie vos trois derniers combats "
                + "(simplifié en jeu : rotation de sorts par défaut de chaque allié).",
            rarity: "Légendaire",
            polarity: "Sévère",
            isMajeure: true,
            minDepth: 3,
            duration: "NextCombatOnly",
            selectionGroup: "law.majeure",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.sablier",
            name: "Loi du Sablier Renversé",
            narrativeText: "Article XLIX — Le temps du Palais n'a pas de sens privilégié. "
                + "Aujourd'hui, il remonte. Les lents ont assez attendu.",
            description: "L'initiative est inversée pour la salle : les combattants les plus "
                + "lents agissent en premier, les plus rapides en dernier. L'ATB coule à l'envers.",
            rarity: "Épique",
            polarity: "Neutre",
            isMajeure: false,
            minDepth: 2,
            duration: "UntilRoomEnds",
            selectionGroup: "law.combat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.devoration",
            name: "Loi de la Dévoration",
            narrativeText: "Article LIX — Le Palais dévore doucement. Il accepte les paiements "
                + "en violence d'autrui. C'est même sa monnaie préférée.",
            description: "Le Palais a faim : chaque salle traversée SANS combat draine 3% des "
                + "PV max de l'équipe. Chaque combat remporté restaure 5%. Nourrissez-le, ou "
                + "nourrissez-vous.",
            rarity: "Épique",
            polarity: "Sévère",
            isMajeure: false,
            minDepth: 2,
            duration: "UntilFloorEnds",
            selectionGroup: "law.combat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.treizieme-coup",
            name: "Loi du Treizième Coup",
            narrativeText: "Article XIII — Toutes les douze frappes, le Palais en réclame une. "
                + "Il ne précise pas pour qui. C'est ce qui rend la chose équitable, et "
                + "divertissante.",
            description: "Un compteur global court sur chaque combat : le 13e coup porté (tous "
                + "camps confondus) inflige des dégâts doublés — et son bénéficiaire est tiré au "
                + "sort parmi TOUS les combattants au moment où il tombe (simplifié en jeu : "
                + "l'auteur naturel du coup en bénéficie).",
            rarity: "Épique",
            polarity: "DoubleTranchant",
            isMajeure: false,
            minDepth: 2,
            duration: "UntilRoomEnds",
            selectionGroup: "law.combat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.destinee",
            name: "Loi de la Destinée",
            narrativeText: "Article C — Il faut parfois savoir chercher au plus profond de soi "
                + "pour repousser ses limites, quel qu'en soit le prix. Le Palais a décidé que "
                + "c'était parfois maintenant, et pour tout le monde.",
            description: "Pour la salle, TOUS les combattants (équipe et ennemis) reçoivent "
                + "« Une destinée cruelle » : +20% Attaque, Défense, Vitesse et Focus "
                + "permanents (durée du combat), -15% sur la vitesse de remplissage ATB — et "
                + "un DoT de 10% des PV max par tour, sans fin.",
            rarity: "Légendaire",
            polarity: "DoubleTranchant",
            isMajeure: true,
            minDepth: 3,
            duration: "UntilRoomEnds",
            selectionGroup: "law.majeure",
            impactDomains: ["Combat"],
            cancellationToken);

        // UpsertLawEffectAsync looks the law up by key via a database query — it must run
        // against a database that already has these 5 laws persisted, not merely added to
        // the change tracker (the final SaveChangesAsync in SeedAsync is too late for that).
        await _ctx.SaveChangesAsync(cancellationToken);

        await UpsertLawEffectAsync("law.reflet", "EnableMirrorCombatCopy", 1m, "NextCombatOnly", null, cancellationToken);
        await UpsertLawEffectAsync("law.sablier", "EnableTurnOrderReverse", 1m, "UntilRoomEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.devoration", "EnableRoomTraversalHpDrain", 1m, "UntilFloorEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.treizieme-coup", "EnableHitCounterDoubleDamage", 1m, "UntilRoomEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.destinee", "EnableCruelDestinyForEveryone", 1m, "UntilRoomEnds", null, cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);
    }

    // Chapitre IV — Lois de combat. 6 of its 7 laws already have full mechanical
    // backing (File Indienne/Curée/Première Impression from Phase 3bis, Duel/Écriture
    // from this pass, Éloge Funèbre new in this pass — post-death basic-attack-only
    // gate via Combat.NextActionRestrictedToBasicAttack). "Loi du Miroir" (copy the
    // ally's first cast onto the fastest enemy, inverted targeting) is NOT seeded —
    // it needs re-entrant skill resolution that doesn't exist.
    //
    // Two room-type weight-doubling promulgation rules are NOT enforced by the engine
    // (documented gap, same as Chapitre VIII): Curée "poids doublé aux Plaines" and
    // Écriture "poids doublé au Palier et au Labyrinthe".
    private async Task SeedLoisDeCombatAsync(CancellationToken cancellationToken)
    {
        await UpsertCompendiumLawAsync(
            key: "law.file-indienne",
            name: "Loi de la File Indienne",
            narrativeText: "Article XVI — Chacun son tour. Le Palais a l'éternité ; vous, un "
                + "peu moins ; raison de plus pour faire la queue.",
            description: "Aucun combattant ne peut agir une seconde fois tant que tous les "
                + "combattants n'ont pas agi une fois. L'ATB devient un tour par tour strict, "
                + "ordonné par Initiative (approximé en jeu : la Vitesse de tous les "
                + "combattants est ramenée à la moyenne du groupe).",
            rarity: "Rare",
            polarity: "Neutre",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.combat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.curee",
            name: "Loi de la Curée",
            narrativeText: "Article XXVII — La pitié est un étage que le Palais n'a jamais "
                + "construit.",
            description: "Tout combattant sous 25% de ses PV subit +15% de dégâts. Le Palais "
                + "achève ce qui chancelle — des deux côtés.",
            rarity: "Peu commun",
            polarity: "Sévère",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.combat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.premiere-impression",
            name: "Loi de la Première Impression",
            narrativeText: "Article VI — On n'a jamais deux fois l'occasion de faire une "
                + "première blessure.",
            description: "Le tout premier coup porté dans chaque combat (quel qu'en soit "
                + "l'auteur) est automatiquement critique. Le Palais n'accorde qu'une seule "
                + "première fois.",
            rarity: "Rare",
            polarity: "DoubleTranchant",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.combat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.duel",
            name: "Loi du Duel",
            narrativeText: "Article XIV — La foule est une lâcheté arithmétique. L'adresse se "
                + "prouve à une seule adresse.",
            description: "Les attaques et sorts mono-cibles infligent +20% ; les attaques et "
                + "sorts de zone infligent -20%. Le Palais estime qu'on ne tue bien qu'en "
                + "regardant.",
            rarity: "Commun",
            polarity: "DoubleTranchant",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.combat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.ecriture",
            name: "Loi de l'Écriture",
            narrativeText: "Article XLVII — Le registre n'efface rien. Il prolonge. C'est sa "
                + "définition, et désormais la vôtre.",
            description: "Tous les DoT (des deux camps) durent +2 tours. Ce qui est écrit au "
                + "Palais y reste écrit un peu plus longtemps.",
            rarity: "Peu commun",
            polarity: "DoubleTranchant",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.combat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.eloge-funebre",
            name: "Loi de l'Éloge Funèbre",
            narrativeText: "Article XXXI — Quand quelqu'un tombe, le Palais exige un instant de "
                + "recueillement. Nul artifice, nul éclat : juste le geste le plus simple.",
            description: "Dès qu'un combattant (allié ou ennemi) est mis hors combat, le "
                + "prochain combattant à agir ne peut porter qu'une attaque de base — aucun "
                + "sort, aucune capacité, tant que ce geste n'a pas été rendu.",
            rarity: "Peu commun",
            polarity: "Sévère",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.combat",
            impactDomains: ["Combat"],
            cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);

        await UpsertLawEffectAsync("law.file-indienne", "EnableTurnOrderLock", 1m, "UntilRoomEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.curee", "EnableLowHpDamageAmplification", 1m, "UntilRoomEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.premiere-impression", "EnableFirstHitCritical", 1m, "UntilFloorEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.duel", "EnableDuelDamageAsymmetry", 1m, "UntilRoomEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.ecriture", "EnableDotDurationExtension", 2m, "UntilFloorEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.eloge-funebre", "EnablePostDeathBasicAttackOnly", 1m, "UntilRoomEnds", null, cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);
    }

    // Chapitre II — Lois climatiques. 4 of its 5 laws are seeded: Marée Haute, Deuil
    // Sec, Voile, Accords. "Loi du Répit" (Accalmie: suspend every active Sévère law
    // for the room) is NOT seeded — it would need a cross-cutting "suspend Sévère
    // RunModifiers" mechanism touching every read site across the codebase, judged
    // too invasive for this pass (documented gap).
    //
    // Loi du Voile's "+10% esquive" half and Loi des Accords' "5% chance de tonner"
    // sub-effect are NOT modeled (documented gap) — no dodge/evasion mechanic and no
    // random-extra-target-on-magic-hit mechanic exist anywhere in the engine; only
    // each law's primary stat effect is seeded. Only one RoomClimate can be active
    // at a time (Run.ReplaceActiveRoomClimateLaws already enforces this), so these 4
    // share a dedicated selection group.
    private async Task SeedLoisClimatiquesAsync(CancellationToken cancellationToken)
    {
        await UpsertCompendiumLawAsync(
            key: "law.voile",
            name: "Loi du Voile",
            narrativeText: "Article VII — Ce que nul ne voit clairement, nul n'a à le regretter "
                + "précisément. Le Palais accorde le flou à parts égales.",
            description: "Climat de la salle : Brume. -3 Focus (brut) pour tous les "
                + "combattants ; +10% d'esquive pour tous les combattants (non modélisé, aucun "
                + "mécanisme d'esquive n'existe dans le moteur — seul le malus de Focus est "
                + "appliqué).",
            rarity: "Commun",
            polarity: "DoubleTranchant",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.climat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.accords",
            name: "Loi des Accords",
            narrativeText: "Article XXIII — Le tonnerre du Palais gronde en accords, jamais en "
                + "coups. Que ceux qui chantent soient entendus plus loin qu'ils ne visaient.",
            description: "Climat de la salle : Orage. +15% dégâts magiques pour tous ; chaque "
                + "sort de dégâts magiques a 5% de chance de « tonner » — frapper une cible "
                + "supplémentaire aléatoire (non modélisé, seul le bonus de dégâts est appliqué).",
            rarity: "Rare",
            polarity: "DoubleTranchant",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.climat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.deuil-sec",
            name: "Loi du Deuil Sec",
            narrativeText: "Article XIX — La forge a brûlé quelque chose aujourd'hui. Le Palais "
                + "porte le deuil, et le deuil ne console pas.",
            description: "Climat de la salle : Pluie de cendres. Tous les soins sont réduits de "
                + "25% ; tous les dégâts de feu gagnent +15% (réinterprété en bonus de dégâts "
                + "DoT — aucun type élémentaire feu n'existe dans le moteur).",
            rarity: "Peu commun",
            polarity: "Sévère",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.climat",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.maree-haute",
            name: "Loi de la Marée Haute",
            narrativeText: "Article XII — La mer a des droits sur toute chose entamée. Ce qui "
                + "saigne saignera un peu plus, par égard pour Elle.",
            description: "Climat de la salle : Pluie violacée. Tous les DoT (joueur et ennemis) "
                + "infligent +1 dégât par tour. (La SFD prévoit aussi qu'à la Falaise la "
                + "probabilité d'apparition de l'Impératrice passe à 100% — non modélisé, "
                + "aucune pondération de rencontre par room-key n'est câblée pour cette loi.)",
            rarity: "Peu commun",
            polarity: "Sévère",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.climat",
            impactDomains: ["Combat"],
            cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);

        await UpsertLawEffectAsync("law.voile", "ApplyRoomClimate", 0m, "UntilRoomEnds", "voile", cancellationToken);
        await UpsertLawEffectAsync("law.accords", "ApplyRoomClimate", 0m, "UntilRoomEnds", "accords", cancellationToken);
        await UpsertLawEffectAsync("law.deuil-sec", "ApplyRoomClimate", 0m, "UntilRoomEnds", "deuil-sec", cancellationToken);
        await UpsertLawEffectAsync("law.maree-haute", "ApplyRoomClimate", 0m, "UntilRoomEnds", "maree-haute", cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);
    }

    // Chapitre III — Lois du seuil & de l'étiquette. 4 of its 5 laws are seeded: Loi des
    // Pieds Essuyés (trivial reuse of StartingGuardBonus), Loi du Silence Dû (new
    // PhysicalDamageBonus/FlatManaCostBonus mechanic), Loi du Tapis Propre (new
    // per-combatant first-turn action-type gate — see Combatant.HasActedThisCombat /
    // CombatSkillActionValidator), and Loi de la Troisième Tasse (new per-heal-application
    // corruption roll — see Combat.ApplyThirdCupRollIfActive, called from both
    // CombatSkillEffectResolver.ResolveHeal and UseItemInCombatCommandHandler.ApplyItemEffect).
    // The last one is NOT seeded — needs a genuinely new engine mechanic that doesn't
    // exist yet: "Loi de l'Invitation" (law.invitation) needs disabling a "flee" combat
    // action that doesn't exist in the engine AND a loot bonus — RunModifierType.
    // RewardPowerMultiplier is itself dead code upstream (written by PalaceLawMapper but
    // never read by RewardOfferFactory, which only consumes the risk-derived
    // CombatRiskProfile.RewardPowerMultiplier), so seeding it would silently do nothing;
    // fixing that pre-existing gap is out of scope here.
    //
    // Loi du Tapis Propre's "poids doublé au Hall et aux Couloirs" promulgation nuance and
    // Loi de la Troisième Tasse's "Le Porteur de Plateau tire sa Tasse retournée à 25% au
    // lieu de 10%" per-NPC-item nuance are NOT enforced (documented gap, same as the
    // room-weight-doubling notes elsewhere). Troisième Tasse's HealAndManaRestorePercent
    // item family is also excluded from the roll (documented simplification, see
    // UseItemInCombatCommandHandler.ApplyItemEffect).
    private async Task SeedLoisDuSeuilAsync(CancellationToken cancellationToken)
    {
        await UpsertCompendiumLawAsync(
            key: "law.pieds-essuyes",
            name: "Loi des Pieds Essuyés",
            narrativeText: "Article II — Quiconque honore le seuil sera tenu pour honorable "
                + "jusqu'à preuve du contraire. La preuve du contraire arrive vite, ici.",
            description: "À l'entrée de chaque combat, toute l'équipe gagne +3 Garde. Le "
                + "seuil respecté protège ceux qui le respectent.",
            rarity: "Commun",
            polarity: "Clémente",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.seuil",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.silence-du",
            name: "Loi du Silence Dû",
            narrativeText: "Article XXXI — Ce qui peut se faire sans être dit gagnera à "
                + "l'être. Le reste paiera le dérangement.",
            description: "Les sorts coûtent +2 mana pour tous les combattants ; les "
                + "attaques physiques infligent +8% de dégâts. Le Palais préfère les "
                + "gestes aux mots.",
            rarity: "Peu commun",
            polarity: "Neutre",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.seuil",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.tapis-propre",
            name: "Loi du Tapis Propre",
            narrativeText: "Article Premier — On s'essuie les pieds. On salue. Ensuite, "
                + "seulement, on peut s'entretuer proprement.",
            description: "Dans chaque combat, le premier tour de chaque combattant "
                + "(équipe ET ennemis) ne peut pas être une attaque : sorts de soutien, "
                + "buffs, débuffs et déplacements uniquement. La politesse d'abord.",
            rarity: "Commun",
            polarity: "Neutre",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.seuil",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.troisieme-tasse",
            name: "Loi de la Troisième Tasse",
            narrativeText: "Article XLIV — Trois tasses sont servies. La première fume, "
                + "la deuxième est vide. Le Palais ne dit jamais laquelle est la troisième.",
            description: "Chaque soin (sort ou objet) a 10% de chance d'être servi dans "
                + "la troisième tasse : il ne restaure que la moitié et applique un "
                + "poison léger (3 dégâts/tour, 4 tours).",
            rarity: "Rare",
            polarity: "Sévère",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.seuil",
            impactDomains: ["Combat"],
            cancellationToken: cancellationToken,
            // "jamais en même temps que l'Édit du Souvenir Doux" per the SFD — mirrors
            // Souvenir Doux's own exclusionKeys entry (see SeedEditsClementsAsync) since
            // the engine checks exclusions one-directionally against the newly-drawn law.
            exclusionKeys: ["law.souvenir-doux"]);

        await _ctx.SaveChangesAsync(cancellationToken);

        await UpsertLawEffectAsync("law.pieds-essuyes", "AddStartingGuard", 3m, "UntilFloorEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.silence-du", "EnableSilenceDuActive", 1m, "UntilRoomEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.tapis-propre", "EnableTapisPropreEnabled", 1m, "UntilFloorEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.troisieme-tasse", "EnableThirdCupHealCorruption", 1m, "UntilFloorEnds", null, cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);
    }

    // Chapitre V — Lois d'économie. 2 of its 4 laws are seeded: "Loi des Poches Cousues"
    // (law.poches-cousues, new RunModifierType.ConsumablesRestrictedInCombat — see
    // Run.UseItem) and "Loi de l'Abondance" (law.abondance, new RunModifierType.
    // AbondanceExtraChoiceEnabled — see RewardOfferFactory.CreateItemRewardOffer). The
    // other 2 are NOT seeded (documented gap): "Loi de l'Impôt du Seuil" (law.impot-seuil)
    // and "Loi du Prêteur" (law.preteur) both need an in-run debit/credit path for Éclats
    // du Palais — today that currency lives ONLY on the player-service PlayerProfile
    // (read via IPlayerProfileGateway as a snapshot value, PalaceShardCount), with no
    // command allowing game-engine to spend or grant it mid-run; wiring a synchronous
    // cross-service wallet mutation into every room transition is out of scope for this
    // pass.
    //
    // Loi de l'Abondance's "un nœud sur deux est vide à l'ouverture" half is NOT modeled
    // (documented simplification) — no zero-choice RewardOffer flow exists; the item
    // node always gets the 4th choice while the law is active, never an empty node.
    private async Task SeedLoisEconomieAsync(CancellationToken cancellationToken)
    {
        await UpsertCompendiumLawAsync(
            key: "law.poches-cousues",
            name: "Loi des Poches Cousues",
            narrativeText: "Article XLI — On ne fouille pas ses poches à table. Ce qui s'y "
                + "trouve mûrira d'attendre.",
            description: "Aucun consommable utilisable en combat dans cette salle. Hors "
                + "combat, les consommables rendent +25% (les soins comme les points).",
            rarity: "Rare",
            polarity: "DoubleTranchant",
            isMajeure: false,
            minDepth: null,
            duration: "UntilRoomEnds",
            selectionGroup: "law.economie",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.abondance",
            name: "Loi de l'Abondance",
            narrativeText: "Article XXXV — L'abondance est un prêt. Le Palais rembourse "
                + "en avance et encaisse en retard.",
            description: "Les nœuds d'objets proposent 4 choix au lieu de 3 — mais un "
                + "nœud sur deux est vide à l'ouverture (le Palais a déjà servi).",
            rarity: "Peu commun",
            polarity: "DoubleTranchant",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.economie",
            impactDomains: ["Rewards"],
            cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);

        await UpsertLawEffectAsync("law.poches-cousues", "EnableConsumablesRestrictedInCombat", 1m, "UntilRoomEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.abondance", "EnableAbondanceExtraChoice", 1m, "UntilFloorEnds", null, cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);
    }

    // Chapitre VII — Édits cléments. 3 of its 5 laws are seeded: Pas Léger (reuses
    // SpeedBonus), Hôte Généreux (reuses StartingGuardBonus), Souvenir Doux (reuses the
    // new AllyHealingBonus mechanic). NOT seeded: "Édit de la Chandelle" (law.chandelle,
    // +1 free reroll on item nodes for the floor — needs a node-reroll-count mechanic
    // that doesn't exist) and "Édit des Portes Ouvertes" (law.portes-ouvertes, reveals the
    // full floor layout — needs a map-reveal feature spanning backend + frontend).
    private async Task SeedEditsClementsAsync(CancellationToken cancellationToken)
    {
        await UpsertCompendiumLawAsync(
            key: "law.pas-leger",
            name: "Édit du Pas Léger",
            narrativeText: "Article VIII — Les couloirs sont longs et le Palais a horreur "
                + "qu'on y traîne. Allez.",
            description: "+10% Vitesse pour toute l'équipe.",
            rarity: "Peu commun",
            polarity: "Clémente",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.edit",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.hote-genereux",
            name: "Édit de l'Hôte Généreux",
            narrativeText: "Article XI — Les invités seront servis avant d'être éprouvés. "
                + "C'est l'ordre des choses, et l'ordre des plats.",
            description: "L'équipe entre dans chaque combat avec +10 Garde.",
            rarity: "Peu commun",
            polarity: "Clémente",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.edit",
            impactDomains: ["Combat"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.souvenir-doux",
            name: "Édit du Souvenir Doux",
            narrativeText: "Article XVII — Il est arrivé, une fois, que quelqu'un soit "
                + "heureux ici. L'étage s'en souvient. Profitez de sa distraction.",
            description: "Tous les soins reçus par l'équipe sont majorés de +20%.",
            rarity: "Peu commun",
            polarity: "Clémente",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.edit",
            impactDomains: ["Combat"],
            cancellationToken: cancellationToken,
            // "incompatible avec la Loi de la Troisième Tasse" per the SFD — mirrored by
            // Troisième Tasse's own exclusionKeys entry (see SeedLoisDuSeuilAsync).
            exclusionKeys: ["law.troisieme-tasse"]);

        await _ctx.SaveChangesAsync(cancellationToken);

        await UpsertLawEffectAsync("law.pas-leger", "ModifySpeed", 0.10m, "UntilFloorEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.hote-genereux", "AddStartingGuard", 10m, "UntilFloorEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.souvenir-doux", "EnableAllyHealingBonus", 20m, "UntilFloorEnds", null, cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);
    }

    // Chapitre VI — Lois de mémoire & de relations. 3 of its 4 laws are seeded: "Loi du
    // Nom Retenu" (reuses ReputationChangeDoubled), "Loi du Témoin" (new
    // WoundHealingBlocked mechanic), and "Loi des Présentations" (new PresentationsEnabled
    // mechanic — see EnemyCombatTurnResolver.Resolve, gated on Combatant.HasActedThisCombat).
    // NOT seeded: "Loi de l'Oubli Partiel" (law.oubli-partiel) needs a mechanism to
    // temporarily remove a random non-Frappe ally skill for the floor plus a floor-end
    // stat-point grant — no skill-removal/floor-end-hook mechanism exists for this shape yet.
    private async Task SeedLoisDeMemoireAsync(CancellationToken cancellationToken)
    {
        await UpsertCompendiumLawAsync(
            key: "law.nom-retenu",
            name: "Loi du Nom Retenu",
            narrativeText: "Article XXV — Ce qui se dit devant témoin compte double. Le "
                + "Palais est toujours témoin.",
            description: "Toute réputation gagnée auprès des PNJ de l'étage est doublée. "
                + "Toute réputation perdue également. Le Palais prend des notes, et il "
                + "écrit gros.",
            rarity: "Rare",
            polarity: "DoubleTranchant",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.memoire",
            impactDomains: ["Narrative"],
            cancellationToken);

        await UpsertCompendiumLawAsync(
            key: "law.temoin",
            name: "Loi du Témoin",
            narrativeText: "Article LVI — Ce qui a été fait a été vu. Ce qui a été vu ne se "
                + "défait pas séance tenante ; il faudra au moins changer d'étage.",
            description: "Les blessures PNJ armées pendant cet étage ne peuvent pas être "
                + "apaisées (ni par acte, ni par score) tant que l'étage n'est pas quitté. "
                + "Les rancunes tiennent.",
            rarity: "Épique",
            polarity: "Sévère",
            isMajeure: false,
            minDepth: 2,
            duration: "UntilFloorEnds",
            selectionGroup: "law.memoire",
            impactDomains: ["Narrative"],
            cancellationToken);

        // "Loi des Présentations" (law.presentations): documented simplification — the
        // SFD's "au premier tour de chaque combat, tous les ennemis annoncent" (a single
        // batch announcement at combat start) is approximated as each enemy announcing
        // individually right before their own first action (see
        // RunModifierType.PresentationsEnabled for the full rationale).
        await UpsertCompendiumLawAsync(
            key: "law.presentations",
            name: "Loi des Présentations",
            narrativeText: "Article IV — Nul ne frappe un inconnu sous ce toit. Faites "
                + "connaissance ; ensuite, frappez qui vous connaissez.",
            description: "Au premier tour de chaque combat, tous les ennemis annoncent "
                + "leur prochaine action (intentions visibles). Version diminuée et "
                + "gratuite des Yeux du marchand, qui eux montrent tout en permanence — "
                + "et coûtent 25% PV max.",
            rarity: "Commun",
            polarity: "Clémente",
            isMajeure: false,
            minDepth: null,
            duration: "UntilFloorEnds",
            selectionGroup: "law.memoire",
            impactDomains: ["Combat"],
            cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);

        await UpsertLawEffectAsync("law.nom-retenu", "EnableReputationChangeDoubled", 1m, "UntilFloorEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.temoin", "EnableWoundHealingBlocked", 1m, "UntilFloorEnds", null, cancellationToken);
        await UpsertLawEffectAsync("law.presentations", "EnablePresentations", 1m, "UntilFloorEnds", null, cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);
    }

    // Chapitre IX — Lois liées aux salles. These are structurally different from every
    // other chapter: they are NOT drawn by the ambient promulgator (AmbientPalaceLawPromulgator
    // explicitly excludes any law with a non-null RoomKey from its pool) and never appear
    // in Run.ActivePalaceLaws — they're "always active" the moment the player is in the
    // matching room, checked directly against the room's RoomKey rather than through a
    // RunModifier. IsCumulExempt = true per the SFD ("elles ne comptent pas dans la
    // limite de cumul : elles sont le terrain, pas le climat").
    //
    // 3 of its 5 laws are seeded: "Loi des Visites Terminées" (room.hopital), "Loi de la
    // Falaise" (room.falaise, added once the Row/Rank positioning system existed — see
    // Combat.FalaiseWindEnabled/ApplyFalaiseWindIfActive), and "Loi du Sanctuaire"
    // (room.meditation, see AmbientPalaceLawPromulgator.SanctuaryRoomKey). All three
    // mechanics are hardcoded room-key checks, not catalog-driven effects — so none has
    // an EffectDefinition/RunModifier attached; they exist purely as descriptive/display
    // metadata. Sanctuaire's SFD text has a second half NOT modeled (documented gap):
    // "aucune loi Sévère ni majeure ne s'applique dans cette salle" — suspending laws
    // already active when entering needs the same RunModifierType.SuspendSevereLaws
    // mechanic "Loi du Répit" (Chapitre II) is missing; only the "no NEW promulgation"
    // half is implemented. The other 2 need mechanics that don't exist yet: "Loi de la
    // Cellule" (room.cellule) needs a no-death/forced-surrender combat-resolution mode;
    // "Loi des Sorties Mouvantes" (room.labyrinthe) needs a chain-combat trigger after
    // 12 turns.
    private async Task SeedLoisLieesAuxSallesAsync(CancellationToken cancellationToken)
    {
        await UpsertCompendiumLawAsync(
            key: "law.visites-terminees",
            name: "Loi des Visites Terminées",
            narrativeText: "Article XLVIII — Les soins sont dispensés par le personnel "
                + "autorisé, aux heures autorisées, aux patients autorisés. Vous n'êtes "
                + "rien de tout cela.",
            description: "Les sorts de soin sont sans effet dans cette salle (les soins "
                + "par objets fonctionnent). L'administration a des horaires ; la magie "
                + "n'a pas de passe-droit.",
            rarity: "Liée",
            polarity: "Sévère",
            isMajeure: false,
            minDepth: null,
            duration: "Permanent",
            selectionGroup: "law.salle",
            impactDomains: ["Combat"],
            cancellationToken: cancellationToken,
            roomKey: "room.hopital",
            isCumulExempt: true);

        // "Loi de la Falaise" (room.falaise): the row/rank positioning system it needs
        // now exists in the engine (CombatFactory sets Combat.FalaiseWindEnabled from
        // the room's RoomKey; Combat.AdvanceTurn resolves the 10% chance each turn) —
        // same hardcoded-RoomKey convention as Visites Terminées above, no
        // EffectDefinition/RunModifier attached.
        await UpsertCompendiumLawAsync(
            key: "law.falaise",
            name: "Loi de la Falaise",
            narrativeText: "Article XL — Entre le Palais et ses enfers, il y a du vent. Le "
                + "vent n'a pas signé le registre. Le vent fait ce qu'il veut.",
            description: "Le vent de la mer violacée souffle : à chaque tour de combat, "
                + "10% de chance qu'un combattant aléatoire soit repoussé d'un rang "
                + "(rang arrière). Les combattants déjà au rang arrière subissent 5 dégâts "
                + "à la place (les embruns).",
            rarity: "Liée",
            polarity: "Sévère",
            isMajeure: false,
            minDepth: null,
            duration: "Permanent",
            selectionGroup: "law.salle",
            impactDomains: ["Combat"],
            cancellationToken: cancellationToken,
            roomKey: "room.falaise",
            isCumulExempt: true);

        // "Loi du Sanctuaire" (room.meditation): AmbientPalaceLawPromulgator now refuses
        // ANY promulgation while the player is in this room (SanctuaryRoomKey check) —
        // same hardcoded-RoomKey convention as the two laws above. The SFD's other half
        // ("aucune loi Sévère ni majeure ne s'applique dans cette salle" — suspending
        // already-active laws) is NOT modeled: it needs the same RunModifierType.
        // SuspendSevereLaws mechanic law.repit (Chapitre II) is missing, documented there.
        await UpsertCompendiumLawAsync(
            key: "law.sanctuaire",
            name: "Loi du Sanctuaire",
            narrativeText: "Article Un-bis — Il existe un endroit où le registre lui-même "
                + "se tait. Le Palais ne s'en souvient que lorsqu'il y entre, et il n'y "
                + "entre jamais.",
            description: "Aucune nouvelle loi ne peut être promulguée tant que l'équipe "
                + "est dans cette salle (les lois Sévères/majeures déjà actives avant "
                + "d'y entrer restent actives — non modélisé : leur suspension a besoin "
                + "du même mécanisme que la Loi du Répit).",
            rarity: "Liée",
            polarity: "Clémente",
            isMajeure: false,
            minDepth: null,
            duration: "Permanent",
            selectionGroup: "law.salle",
            impactDomains: ["Narrative"],
            cancellationToken: cancellationToken,
            roomKey: "room.meditation",
            isCumulExempt: true);

        await _ctx.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertCompendiumLawAsync(
        string key, string name, string narrativeText, string description,
        string rarity, string polarity, bool isMajeure, int? minDepth, string duration,
        string selectionGroup, string[] impactDomains, CancellationToken cancellationToken,
        int? maxDepth = null, string? roomKey = null, bool isCumulExempt = false,
        string[]? exclusionKeys = null, int baseWeight = 1)
    {
        const string version = "compendium-1.0.0";
        var now = DateTime.UtcNow;
        var domainsJson = JsonSerializer.Serialize(impactDomains);
        var exclusionKeysJson = JsonSerializer.Serialize(exclusionKeys ?? []);

        var existing = await _ctx.PalaceLawDefinitions.FirstOrDefaultAsync(l => l.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.PalaceLawDefinitions.Add(new PalaceLawDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name,
                DisplayName = name,
                Description = description,
                NarrativeText = narrativeText,
                Version = version,
                Status = "Active",
                Scope = "Run",
                Duration = duration,
                Trigger = "OnApplied",
                Severity = polarity == "Sévère" || polarity == "DoubleTranchant" ? 2 : 1,
                Visibility = "Visible",
                Priority = 0,
                EffectSetId = null,
                BaseWeight = baseWeight,
                MinDepth = minDepth,
                MaxDepth = maxDepth,
                SelectionGroup = selectionGroup,
                ImpactDomainsJson = domainsJson,
                Rarity = rarity,
                Polarity = polarity,
                IsMajeure = isMajeure,
                RoomKey = roomKey,
                IsCumulExempt = isCumulExempt,
                ExclusionKeysJson = exclusionKeysJson,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }

        existing.Name = name; existing.DisplayName = name;
        existing.Description = description; existing.NarrativeText = narrativeText;
        existing.Version = version; existing.Status = "Active";
        existing.Duration = duration; existing.Priority = 0;
        existing.MinDepth = minDepth; existing.MaxDepth = maxDepth;
        existing.SelectionGroup = selectionGroup; existing.ImpactDomainsJson = domainsJson;
        existing.Rarity = rarity; existing.Polarity = polarity; existing.IsMajeure = isMajeure;
        existing.RoomKey = roomKey; existing.IsCumulExempt = isCumulExempt;
        existing.ExclusionKeysJson = exclusionKeysJson; existing.BaseWeight = baseWeight;
        existing.UpdatedAtUtc = now;
    }

    // ── SALLES CANON (lieux) ──────────────────────────────────────────────────
    private async Task SeedCanonRoomTypesAsync(CancellationToken cancellationToken)
    {
        // key, theme (= valeur d'état Markov & jointure avec RoomDefinition.Theme), minDepth
        await UpsertRoomTypeAsync("room-type.threshold", "Seuil", "Threshold", 0, cancellationToken);
        await UpsertRoomTypeAsync("room-type.memory", "Mémoire", "Memory", 1, cancellationToken);
        await UpsertRoomTypeAsync("room-type.rupture", "Rupture", "Rupture", 1, cancellationToken);
        await UpsertRoomTypeAsync("room-type.silence", "Silence", "Silence", 1, cancellationToken);
        await UpsertRoomTypeAsync("room-type.fear", "Effroi", "Fear", 1, cancellationToken);
        await UpsertRoomTypeAsync("room-type.forest", "Forêt", "Forest", 1, cancellationToken);
        await UpsertRoomTypeAsync("room-type.antechamber", "Antichambre", "Antechamber", 3, cancellationToken);
    }

    private async Task UpsertRoomTypeAsync(string key, string displayName, string theme, int minDepth, CancellationToken cancellationToken)
    {
        var existing = await _ctx.RoomTypeDefinitions.FirstOrDefaultAsync(r => r.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.RoomTypeDefinitions.Add(new RoomTypeDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = displayName,
                Description = displayName,
                Theme = theme,
                DefaultRarity = "Common",
                MinDepth = minDepth,
                Version = "canon-1.0.0",
                Status = "Active"
            });
            return;
        }
        existing.DisplayName = displayName; existing.Theme = theme; existing.MinDepth = minDepth;
        existing.Status = "Active";
    }

    private async Task SeedCanonRoomsAsync(CancellationToken cancellationToken)
    {
        // key, name, desc, family, rarity, theme, depthMin, depthMax
        await UpsertRoomAsync("canon.room.pistburg", "Pistburg",
            "La cité-cuvette pestiférée. Brume tenace, marées violentes, prières qui montent des ruelles.",
            "World", "Common", "Fear", 1, 3, cancellationToken);

        await UpsertRoomAsync("canon.room.cathedrale", "La cathédrale",
            "Marbre chaud et sec malgré l'humidité. Les figures de pierre te jugent. L'intérieur est condamné.",
            "World", "Uncommon", "Fear", 2, 5, cancellationToken);

        await UpsertRoomAsync("canon.room.citadelle-papale", "La citadelle papale",
            "Le siège du pouvoir lituique. On n'y entre que pour en découdre.",
            "World", "Rare", "Fear", 4, 8, cancellationToken);

        await UpsertRoomAsync("canon.room.abbaye-tour", "L'abbaye et la tour",
            "La veille. À la nuit, les prières impies s'élèvent et quelque chose y répond.",
            "World", "Uncommon", "Fear", 2, 6, cancellationToken);

        await UpsertRoomAsync("canon.room.mounkaanet", "Le temple de Mounkaanêt",
            "Couloirs gravés de symboles, une porte de pierre qui n'a pas de poignée. L'expédition descend.",
            "World", "Rare", "Memory", 3, 7, cancellationToken);

        await UpsertRoomAsync("canon.room.nooolut", "No'Oolut",
            "La nature reprend ses droits. Une flore intrigante, des bêtes sans peur de l'homme. L'éveil par la survie.",
            "World", "Uncommon", "Silence", 2, 6, cancellationToken);

        await UpsertRoomAsync("canon.room.jardin", "Le jardin",
            "L'immortelle symphonie. Un répit — ou un piège qui a la patience des fleurs.",
            "World", "Rare", "Memory", 3, 8, cancellationToken);

        await UpsertRoomAsync("canon.room.jura", "La grotte du Jura",
            "Fièvre, hallucinations, vibrations dans la roche. La faille où le réel se fendille.",
            "World", "Rare", "Rupture", 3, 7, cancellationToken);

        await UpsertRoomAsync("canon.room.puszta", "Le désert de la Puszta",
            "L'origine du Lituisme. Le sable garde la mémoire du premier mensonge.",
            "World", "Rare", "Fear", 4, 9, cancellationToken);

        await UpsertRoomAsync("canon.room.mlionat", "M'Lionât",
            "La ville prophétique. « Le cœur du cœur. » Son nom même est en araméen.",
            "World", "Epic", "Memory", 5, 9, cancellationToken);

        await UpsertRoomAsync("canon.room.chateau-homoncule", "Le château de l'Homoncule",
            "Un labyrinthe infernal de chaînes dorées virant au bordeaux. Seul le fil d'Ariane mène au cœur.",
            "PalaceCore", "Epic", "Rupture", 5, 9, cancellationToken);
    }

    private async Task UpsertRoomAsync(
        string key, string name, string description,
        string family, string rarity, string theme,
        int depthMin, int depthMax, CancellationToken cancellationToken,
        bool triggersStrictChain = false,
        bool excludeFromOpenPool = false,
        string reachabilityMode = "Explicit",
        bool isCulturalEcho = true)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var existing = await _ctx.RoomDefinitions.FirstOrDefaultAsync(r => r.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.RoomDefinitions.Add(new RoomDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = name,
                Description = description,
                NarrativeText = description,
                RoomFamily = family,
                RoomRarity = rarity,
                Theme = theme,
                MinDepth = depthMin,
                MaxDepth = depthMax,
                BaseWeight = 1,
                SelectionGroup = "room.canon",
                EnemyPoolKey = null,
                RewardPoolKey = null,
                LawPoolKey = null,
                CursePoolKey = null,
                IsUnique = rarity == "Epic",
                IsCulturalEcho = isCulturalEcho,
                TriggersStrictChain = triggersStrictChain,
                ExcludeFromOpenPool = excludeFromOpenPool,
                ReachabilityMode = reachabilityMode,
                Version = version,
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }
        existing.DisplayName = name; existing.Description = description; existing.NarrativeText = description;
        existing.RoomFamily = family; existing.RoomRarity = rarity; existing.Theme = theme;
        existing.MinDepth = depthMin; existing.MaxDepth = depthMax;
        existing.TriggersStrictChain = triggersStrictChain;
        existing.ExcludeFromOpenPool = excludeFromOpenPool;
        existing.ReachabilityMode = reachabilityMode;
        existing.Version = version; existing.Status = "Active"; existing.UpdatedAtUtc = now;
    }

    /// <summary>
    /// Monde "Palais" (contenu bêta). Contrairement à SeedCanonRoomsAsync (Pittsburgh /
    /// L'épopée des Échos, hors périmètre de la bêta), ces salles portent le graphe de
    /// réachabilité explicite décrit dans la SFD "Refonte des Rooms".
    /// Seules les trois chaînes strictes confirmées sont câblées ici (Falaise→Enfers,
    /// Soleil→Château, Hôpital→Cellule) : les listes niveau 1 ↔ niveau 1 et la liste
    /// d'exclusion de room.couloirs restent des TODO éditoriaux (SFD Annexe B, #2 et #3)
    /// — ne pas inventer ce contenu narratif ici.
    /// </summary>
    private static readonly string[] PalaisRoomKeys =
    [
        "room.halldentree", "room.palier", "room.couloirs", "room.feelings", "room.turtle",
        "room.enfermement", "room.meditation", "room.room08", "room.labyrinthe", "room.chambredelise",
        "room.jardin", "room.faille",
        "room.falaise", "room.enfer1", "room.enfer2", "room.enfer3", "room.enfer4",
        "room.soleil", "room.chateau", "room.cellule",
        "room.hopital", "room.cellulehopital",
        "room.montagne", "room.templempontagne", "room.chambrefunéraire",
        "room.sousterrainmontagne", "room.cavernedecrystal"
    ];

    private async Task SeedPalaisWorldAsync(CancellationToken cancellationToken)
    {
        await UpsertRoomAsync("room.halldentree", "Hall d'entrée",
            "Depuis toujours le Palais a su accueillir ses invités. Couvert d'un grand tapis rouges et habillé de " +
            "quatres merveilleux pilier de marbre, le Hall d'entrée du Palais n'est que la représentation de " +
            "l'arrogance de son propriétaire. Une fois traversé, rares sont les personnes qui ont eu l'occasion " +
            "de le revoir.",
            "Palais intérieur", "Epic", "Welcome", 0, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.palier", "Palier",
            "Situé juste après le hall d'entrée, le palier n'est accessible qu'à ceux qui auront su gravir les 8 " +
            "marches qui séparent les deux pièces. 8 marches qui semblent une éternité pour ceux qui empruntent " +
            "cette voie, se retrouvant finalement face à un immense livre s'écrivant seul.",
            "Palais intérieur", "Rare", "Memory", 1, 9, cancellationToken, isCulturalEcho: false);

        await UpsertRoomAsync("room.couloirs", "Couloirs",
            "Distordue, parfois suintant, parfois envahi d'entités monstrueuses, les couloirs sont les chemins à " +
            "suivre pour espérer pouvoir pénétrer dans une pièce. Le tapis bordeaux qui habille le sol n'est pas " +
            "sans rappeler que vous êtes proche du Hall, sans pouvoir l'atteindre.",
            "Palais intérieur", "Common", "Silence", 1, 9, cancellationToken,
            reachabilityMode: "AllExceptListed", isCulturalEcho: false);

        await UpsertRoomAsync("room.feelings", "Pièce des émotions",
            "Autrefois une simple chambre accueillant les invités, l'Architecte a, lors de la seconde " +
            "reconstruction du Palais, adapté cette pièce pour qu'elle n'accueille qu'un seul type d'invité : " +
            "les émotions, et ceux, dans le maigre espoir qu'elles puissent se sentir chez elle dans le Palais. " +
            "Aujourd'hui, cette pièce n'est plus remplie que par quelques échos d'émotions, ou quelques objets " +
            "laissés ici et là par les anciens locataires.",
            "Palais intérieur", "Uncommon", "Feelings", 2, 9, cancellationToken, isCulturalEcho: false);

        await UpsertRoomAsync("room.turtle", "Passage brisé, vers la tortue",
            "Dans des temps anciens, bien avant la seconde reconstruction, il semblait exister un lien entre le " +
            "Palais et une autre entité tout aussi grande et imposante. Désormais brisé, le lien qui permettait " +
            "autrefois aux habitants de chaque côté de se rejoindre n'est plus qu'une faille dans l'immensité du " +
            "Palais. Peu sont les invités ayant pu contempler cette faille violacée.",
            "Palais intérieur", "Epic", "Collapse", 2, 9, cancellationToken, isCulturalEcho: false);

        await UpsertRoomAsync("room.enfermement", "Pièce camisolée",
            "Alors qu'il devenait fou, l'architecte a bâti un système de sécurité archaïque, dans l'urgence " +
            "d'une mort proche. Bâtie de murs renforcés, une simple porte d'acier que seule Elise peut ouvrir de " +
            "l'extérieur, cette pièce existe pour isoler tous ceux qui oseront y pénétrer.",
            "Palais intérieur", "Rare", "Confinement", 2, 9, cancellationToken, isCulturalEcho: false);

        await UpsertRoomAsync("room.meditation", "Salle de méditation",
            "Située au sommet du Palais, côtoyant les cieux, cette pièce apaise les êtres qui y entrent.",
            "Palais intérieur", "Uncommon", "Meditation", 2, 9, cancellationToken, isCulturalEcho: false);

        await UpsertRoomAsync("room.room08", "Chambre 08",
            "Parmi l'infinité de pièces que contient le Palais, la chambre 08 a une histoire toute particulière, " +
            "et une habitante tout aussi unique : Hitomi. Longuement maintenue close pour laisser le temps à " +
            "cette femme de se soigner après les brûlures qu'elle a subies, cette pièce est aujourd'hui ouverte " +
            "et y croiser Hitomi relève de la chance.",
            "Palais intérieur", "Common", "Peace", 2, 9, cancellationToken, isCulturalEcho: false);

        await UpsertRoomAsync("room.labyrinthe", "Labyrinthe",
            "Maintenu enfermé, protégé par les sinueux couloirs, le labyrinthe abrite le premier livre, celui qui " +
            "a permis au Palais de devenir infini et d'écrire l'histoire des habitants d'origine. Y rentrer n'est " +
            "pas le plus difficile, mais en sortir sans le fil d'Ariane relève du défi.",
            "Palais intérieur", "Rare", "Memory", 3, 9, cancellationToken, isCulturalEcho: false);

        await UpsertRoomAsync("room.chambredelise", "Chambre d'Elise",
            "En dehors des couloirs, loin de l'entrée du Palais, la chambre d'Elise date d'avant la construction " +
            "du Palais. Bâtie au début dans le cœur de l'architecte, cette chambre ne servait qu'à contenir " +
            "avidement une créature aussi belle qu'essentielle au fonctionnement de ce dernier. Lors de la " +
            "seconde reconstruction, la décision du conseil fut prise de la libérer et de la laisser vivre " +
            "librement. Même si Elise n'est presque jamais dans sa chambre, bien des évènements peuvent y " +
            "survenir, et des créatures y apparaître.",
            "Palais intérieur", "Epic", "Feelings", 2, 9, cancellationToken, isCulturalEcho: false);

        await UpsertRoomAsync("room.jardin", "Le jardin",
            "Entourant le Palais, le jardin ressemble à tout ce dont toute personne pourrait rêver : des fleurs " +
            "merveilleuses, une ambiance calme et sereine et des habitants qui s'y promènent, sifflotant et " +
            "discutant tranquillement.",
            "Palais intérieur", "Common", "Peace", 2, 9, cancellationToken, isCulturalEcho: false);

        await UpsertRoomAsync("room.faille", "La faille",
            "Centre de l'univers du Palais, la faille est le point névralgique de toutes les dimensions que le " +
            "Palais a su accueillir. Lors de la seconde reconstruction, une implosion a eu lieu dans le cœur du " +
            "Palais. Lorsque l'architecte et le conservateur allèrent vérifier, le cœur avait disparu et une " +
            "faille violacée le remplaçait. C'est à cet instant que fut créé l'aventurier, n'ayant pour seule " +
            "mission que de pénétrer dans cette faille et explorer les différents univers qui s'offrent à lui.",
            "Palais intérieur", "Common", "Silence", 6, 9, cancellationToken, isCulturalEcho: false);

        // Chaîne stricte : Falaise → Enfer1 → Enfer2 → Enfer3 → Enfer4 → (repli niveau 0)
        await UpsertRoomAsync("room.falaise", "La falaise",
            "Seul passage vers les enfers, cette falaise surplombe la mer violacée qui sépare le Palais des " +
            "enfers. Malheureux sont ceux qui croiseront l'impératrice dans ce lieu.",
            "Palais intérieur", "Common", "Fear", 2, 9, cancellationToken,
            triggersStrictChain: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.enfer1", "Les enfers - La calamité",
            "La calamité, le premier étage des Enfers. Composé d'une terre dévastée, hantée par les squelettes " +
            "des souvenirs morts, ce lieu est aussi cruel par son hostilité que par le silence pesant qui y règne.",
            "Palais intérieur", "Uncommon", "Silence", 3, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.enfer2", "Les enfers - la plaine",
            "Calme, silencieuse, habitée d'animaux et autres chimères, les plaines sont le reflet des créations " +
            "de l'architecte. Mais le calme apparent laisse rapidement place à des ordres qui ne demandent qu'une " +
            "seule chose : se nourrir.",
            "Palais intérieur", "Uncommon", "Madness", 3, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.enfer3", "Les enfers - la forge",
            "Des marteaux qui hurlent, une forge qui recrache de la fumée et des créations inachevées qui errent " +
            "sans but sur les plaques d'acier et les piliers de fer qui décorent cet étage. Le forgeron guette, " +
            "crée et rejette ses propres créations.",
            "Palais intérieur", "Uncommon", "Terrify", 3, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.enfer4", "Les enfers - Le chateau",
            "Dernier étage connu des enfers, le chateau fut longtemps la résidence de l'Homoncule. Son sol, " +
            "souillé par les milliers de soldats morts pour sauver l'enfant prisonnier, hurle encore de désespoir " +
            "et de souffrance.",
            "Palais intérieur", "Uncommon", "Collapse", 3, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        // Chaîne stricte : Soleil → Château → Cellule → (repli niveau 0)
        await UpsertRoomAsync("room.soleil", "Le soleil",
            "Un astre cosmique et, en son centre, un chateau. Le soleil n'est, comme tout ce qui habite le " +
            "Palais, qu'une simple pièce aux dimensions immenses.",
            "Palais intérieur", "Rare", "Feelings", 4, 9, cancellationToken,
            triggersStrictChain: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.chateau", "Le chateau",
            "Autrefois situé dans le quatrième étage des enfers, le chateau se trouve désormais au centre du " +
            "soleil, alimentant le plasma et le rayonnement de cet astre qui réchauffe le Palais.",
            "Palais intérieur", "Rare", "Peace", 4, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.cellule", "Le chateau - La cellule",
            "Une pièce, une seule et unique pièce de ce chateau porte toute l'histoire du Palais. À l'intérieur, " +
            "des jeux d'enfants, des coloriages et des dessins sur le mur, un simple lit et le souvenir d'un " +
            "petit être qui créa la première version du Palais, bien avant que l'Architecte ne vienne imposer " +
            "ses plans.",
            "Palais intérieur", "Epic", "Memory", 5, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        // Chaîne stricte : Hôpital → Cellule de l'hôpital → (repli niveau 0)
        await UpsertRoomAsync("room.hopital", "L'hopital",
            "Blanc, vide d'émotions, une odeur de produit ménager et uniquement habité de souvenirs et de " +
            "regrets, l'hopital du Palais a longtemps accueilli les âmes errantes et les avatars mourants. " +
            "Aujourd'hui, il existe encore même si y pénétrer n'est que peu enviable.",
            "Palais intérieur", "Rare", "Madness", 4, 9, cancellationToken,
            triggersStrictChain: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.cellulehopital", "L'hopital - la cellule",
            "Au même titre que la chambre du chateau a longtemps accueilli l'enfant, la cellule de l'hopital fut " +
            "construite sur mesure pour l'Architecte, juste avant la seconde reconstruction du Palais. Plongé " +
            "dans une folie sans nom, submergé par les émotions et les échos, il fut interné après avoir voulu " +
            "détruire le livre situé dans le Labyrinthe.",
            "Palais intérieur", "Epic", "Madness", 5, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        // Chaîne stricte : Montagne → Temple → Chambre funéraire → Sous-terrains →
        // Caverne de crystal → (repli niveau 0). Fournie par l'utilisateur pour
        // débloquer les familles Bestiaire "Pénitents de la Montagne" et "Gardiens
        // de Crystal", dont les créatures pointaient vers des salles inexistantes.
        await UpsertRoomAsync("room.montagne", "La montagne",
            "Paysage de calme, de retraite et d'apaisement, les montagnes du Palais sont un lieu de repentance " +
            "pour tous ceux qui souhaitent effectuer un pèlerinage en toute quiétude.",
            "Palais intérieur", "Common", "Meditation", 2, 9, cancellationToken,
            triggersStrictChain: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.templempontagne", "La montagne - Le temple",
            "Contemplant les montagnes et les plaines, le temple des montagnes impressionne par sa structure " +
            "Maya, sa taille déraisonnable et, surtout, ses pièces aux piliers ornés de joyaux et de gravures " +
            "anciennes.",
            "Palais intérieur", "Common", "Silence", 2, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.chambrefunéraire", "La montagne - la chambre funéraire",
            "Au centre du temple, une vision d'horreur se réveille. La chambre funéraire du premier explorateur " +
            "fut découverte lors de la première reconstruction du Palais, par un aventurier accompagné de " +
            "Hitomi et, depuis, les échos de la frayeur ne cessent de s'agiter au sein de cette pièce.",
            "Palais intérieur", "Rare", "Underground", 2, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.sousterrainmontagne", "La montagne - Les sous-terrains",
            "Derrière la chambre funéraire, cachée par une porte qui ne s'ouvre que si l'on est digne des " +
            "profondeurs, se trouve un long tunnel qui mène à une antichambre, qui mène à un lieu unique et " +
            "magnifique : la chambre de crystal.",
            "Palais intérieur", "Epic", "Underground", 2, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        await UpsertRoomAsync("room.cavernedecrystal", "La montagne - La caverne de crystal",
            "Pièce antique, datée de la construction du Palais, la caverne de crystal abrite bien plus que de " +
            "simples joyaux resplendissants. Une magie ancestrale, des gardiens intemporels et, au milieu de " +
            "tout cela, une sorte de vieille maison continuellement en feu.",
            "Palais intérieur", "Legendary", "Collapse", 2, 9, cancellationToken,
            excludeFromOpenPool: true, isCulturalEcho: false);

        // Les salles doivent être persistées avant de résoudre leurs Id par clé pour le
        // câblage du graphe et du Monde ci-dessous (les requêtes suivantes touchent la
        // base, elles ne voient pas les Add() en attente sur le change tracker).
        await _ctx.SaveChangesAsync(cancellationToken);

        await LinkStrictChainAsync(cancellationToken,
            "room.falaise", "room.enfer1", "room.enfer2", "room.enfer3", "room.enfer4");
        await LinkStrictChainAsync(cancellationToken,
            "room.soleil", "room.chateau", "room.cellule");
        // L'hôpital ne s'arrête pas à sa cellule : elle mène ensuite à la faille (seule
        // salle niveau 1 qui referme cette chaîne plutôt que de retomber sur le hall).
        await LinkStrictChainAsync(cancellationToken,
            "room.hopital", "room.cellulehopital", "room.faille");
        await LinkStrictChainAsync(cancellationToken,
            "room.montagne", "room.templempontagne", "room.chambrefunéraire",
            "room.sousterrainmontagne", "room.cavernedecrystal");

        // Enchaînements niveau 1 ↔ niveau 2 (source : tableau "Room" fourni, colonne
        // "Pièce suivante" faisant foi salle par salle — cf. discussion sur les quelques
        // incohérences mineures du tableau source, ex. room.palier ne liste pas
        // room.room08 alors que room08 le cite comme prédécesseur : room08 reste
        // atteignable via room.couloirs de toute façon).
        //
        // room.halldentree : toutes les salles de niveau 1, + l'exception explicite
        // room.hopital (niveau 2) que le tableau source rattache directement au hall.
        await LinkReachabilityAsync("room.halldentree", "room.palier", cancellationToken);
        await LinkReachabilityAsync("room.halldentree", "room.couloirs", cancellationToken);
        await LinkReachabilityAsync("room.halldentree", "room.feelings", cancellationToken);
        await LinkReachabilityAsync("room.halldentree", "room.turtle", cancellationToken);
        await LinkReachabilityAsync("room.halldentree", "room.enfermement", cancellationToken);
        await LinkReachabilityAsync("room.halldentree", "room.jardin", cancellationToken);
        await LinkReachabilityAsync("room.halldentree", "room.hopital", cancellationToken);

        // room.couloirs reste en liste noire (ReachabilityMode = AllExceptListed, déjà
        // configuré) : comme toutes les salles niveau 3+ sont déjà marquées
        // ExcludeFromOpenPool, la résolution automatique donne exactement "toutes les
        // salles de niveau 1 et 2" sans qu'aucune exclusion explicite soit nécessaire ici.
        // room.montagne (ExcludeFromOpenPool: false) en profite automatiquement — pas
        // de LinkReachabilityAsync explicite nécessaire pour l'atteindre depuis les
        // couloirs.

        await LinkReachabilityAsync("room.palier", "room.couloirs", cancellationToken);
        await LinkReachabilityAsync("room.palier", "room.meditation", cancellationToken);

        await LinkReachabilityAsync("room.labyrinthe", "room.falaise", cancellationToken);
        await LinkReachabilityAsync("room.labyrinthe", "room.faille", cancellationToken);

        await LinkReachabilityAsync("room.jardin", "room.soleil", cancellationToken);

        // room.feelings, room.turtle, room.enfermement, room.meditation, room.room08 et
        // room.chambredelise n'ont volontairement aucun enfant déclaré dans le tableau
        // source : ce sont des culs-de-sac qui renvoient au hall d'entrée (SFD § 5.4).

        await UpsertWorldAsync("palais", "Palais", "room.halldentree", cancellationToken);
        await _ctx.SaveChangesAsync(cancellationToken);

        var worldId = await _ctx.WorldDefinitions
            .Where(w => w.Key == "palais")
            .Select(w => w.Id)
            .FirstAsync(cancellationToken);
        var palaisRooms = await _ctx.RoomDefinitions
            .Where(r => PalaisRoomKeys.Contains(r.Key))
            .ToListAsync(cancellationToken);
        foreach (var room in palaisRooms)
        {
            room.WorldDefinitionId = worldId;
        }
    }

    /// <summary>
    /// Câble une chaîne stricte : chaque salle listée n'a que la suivante pour seul enfant
    /// valide. La dernière salle de la liste n'a volontairement aucun enfant déclaré — ce
    /// qui déclenche le repli vers la salle de niveau 0 du Monde (SFD § 5.4).
    /// </summary>
    private async Task LinkStrictChainAsync(CancellationToken cancellationToken, params string[] roomKeysInOrder)
    {
        for (var i = 0; i < roomKeysInOrder.Length - 1; i++)
        {
            await LinkReachabilityAsync(roomKeysInOrder[i], roomKeysInOrder[i + 1], cancellationToken);
        }
    }

    private async Task LinkReachabilityAsync(string fromKey, string toKey, CancellationToken cancellationToken)
    {
        var fromId = await _ctx.RoomDefinitions.Where(r => r.Key == fromKey).Select(r => r.Id).FirstAsync(cancellationToken);
        var toId = await _ctx.RoomDefinitions.Where(r => r.Key == toKey).Select(r => r.Id).FirstAsync(cancellationToken);

        var exists = await _ctx.RoomReachability.AnyAsync(
            r => r.FromRoomDefinitionId == fromId && r.ToRoomDefinitionId == toId, cancellationToken);
        if (!exists)
        {
            _ctx.RoomReachability.Add(new RoomReachabilityEntity { FromRoomDefinitionId = fromId, ToRoomDefinitionId = toId });
        }
    }

    private async Task UpsertWorldAsync(string key, string displayName, string entryRoomKey, CancellationToken cancellationToken)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var entryRoomId = await _ctx.RoomDefinitions.Where(r => r.Key == entryRoomKey).Select(r => r.Id).FirstAsync(cancellationToken);

        var existing = await _ctx.WorldDefinitions.FirstOrDefaultAsync(w => w.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.WorldDefinitions.Add(new WorldDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                DisplayName = displayName,
                EntryRoomDefinitionId = entryRoomId,
                Version = version,
                Status = "Active",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }
        existing.DisplayName = displayName; existing.EntryRoomDefinitionId = entryRoomId;
        existing.Version = version; existing.Status = "Active"; existing.UpdatedAtUtc = now;
    }

    /// <summary>
    /// Sparse editorial overrides for the theme-affinity matrix (SFD § 5.3): only the
    /// pairs listed here deviate from the default weight applied to every other theme
    /// combination — no need to fill the full N×N grid for the 18-theme canon vocabulary
    /// (Welcome, Religion, Feelings, Fear, Terrify, Peace, Meditation, Silence,
    /// Underground, Madness, Collapse, Garden, Forest, Confinement, Corridors, Hell,
    /// Heaven, Memory). Adding a new theme later needs zero entries here to work; add a
    /// row only when a specific transition should be reinforced or avoided narratively.
    /// </summary>
    private async Task SeedRoomThemeAffinitiesAsync(CancellationToken cancellationToken)
    {
        // Escalation toward dread.
        await UpsertRoomThemeAffinityAsync("Fear", "Terrify", 2.5m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Terrify", "Madness", 2.5m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Madness", "Collapse", 2.0m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Collapse", "Hell", 2.0m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Confinement", "Madness", 2.0m, cancellationToken);

        // Calm / introspective chain.
        await UpsertRoomThemeAffinityAsync("Peace", "Meditation", 2.5m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Meditation", "Silence", 2.0m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Garden", "Peace", 2.5m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Heaven", "Peace", 2.0m, cancellationToken);

        // Entering the Palais and settling into its memory/emotion register.
        await UpsertRoomThemeAffinityAsync("Welcome", "Memory", 2.0m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Memory", "Feelings", 2.0m, cancellationToken);

        // Natural / liminal descent.
        await UpsertRoomThemeAffinityAsync("Forest", "Underground", 2.0m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Underground", "Confinement", 2.0m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Corridors", "Silence", 1.5m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Silence", "Corridors", 1.5m, cancellationToken);

        // Religion bridges toward either extreme, tilted slightly toward Heaven.
        await UpsertRoomThemeAffinityAsync("Religion", "Heaven", 2.0m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Religion", "Hell", 1.5m, cancellationToken);

        // Tonal opposites: rarely follow one another directly, never impossible.
        await UpsertRoomThemeAffinityAsync("Peace", "Hell", 0.2m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Heaven", "Hell", 0.15m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Welcome", "Hell", 0.2m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Garden", "Confinement", 0.3m, cancellationToken);
        await UpsertRoomThemeAffinityAsync("Meditation", "Madness", 0.2m, cancellationToken);
    }

    private async Task UpsertRoomThemeAffinityAsync(
        string themeFrom, string themeTo, decimal weight, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var existing = await _ctx.RoomThemeAffinities.FirstOrDefaultAsync(
            a => a.ThemeFrom == themeFrom && a.ThemeTo == themeTo, cancellationToken);

        if (existing is null)
        {
            _ctx.RoomThemeAffinities.Add(new RoomThemeAffinityEntity
            {
                Id = Guid.NewGuid(),
                ThemeFrom = themeFrom,
                ThemeTo = themeTo,
                Weight = weight,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }

        existing.Weight = weight;
        existing.UpdatedAtUtc = now;
    }
    // ── BOSS CANON ────────────────────────────────────────────────────────────
    // Crée à la fois l'EnemyDefinition (IsBoss) + StatBlock + le RoomBossDefinition.
    private async Task SeedCanonBossesAsync(CancellationToken cancellationToken)
    {
        // Every room type generates a RoomBoss node in its final row (see MapRoomGenerator),
        // so every room type MUST have a RoomBossDefinition or run/room generation throws
        // ("No boss profile found for room type '<X>'"). Only Antechamber/Rupture had canon
        // bosses assigned; Threshold/Memory/Silence/Fear/Forest had none at all, which broke
        // room generation for those types outright. Each existing boss now also covers one of
        // the previously-uncovered room types (first entry in `roomTypes` is the original,
        // unchanged assignment — kept first so its RoomBossDefinition key stays stable).
        // enemyKey, bossKey, name, desc, roomTypes, danger, difficulty, vit, atk, def, guard, speed, skillKey
        // Defense authored at roughly the same order of magnitude as Attack (harmonized
        // ×3 pass) so it carries real weight in the Attack/Defense damage ratio once both
        // scale with room depth; boss Focus derives from difficulty (×2) instead of a flat value.
        await UpsertBossAsync(
            "canon.enemy.grand-cardinal",
            "canon.boss.grand-cardinal",
            "Le Grand Cardinal",
            "Le grand cardinal du Palais",
            new[] { "Antechamber", "Threshold" },
            "75",
            2, 90, 14, 18, 6, 12,
            new[]
                { "canon.skill.priere-aspiration", "canon.skill.flamme-froide", "skill.basic.strike" },
            cancellationToken);

        await UpsertBossAsync("canon.enemy.imperatrice-vipere", "canon.boss.imperatrice-vipere", "L'Impératrice — la Vipère", "L'impératrice du Palais",
            new[] { "Rupture", "Memory" }, "75", 3, 140, 20, 24, 8, 14,
            new[] { "canon.skill.priere-aspiration", "canon.skill.flamme-froide", "skill.basic.strike" }, cancellationToken);

        await UpsertBossAsync("canon.enemy.homoncule-roi", "canon.boss.homoncule-roi", "L'Homoncule — le Vieillard", "Le roi, l'Homoncule, bien des nom lui furent donné",
            new[] { "Rupture", "Silence" }, "75", 3, 160, 22, 27, 10, 8,
            new[] { "canon.skill.transmutation", "canon.skill.flamme-froide", "skill.basic.strike" }, cancellationToken);

        await UpsertBossAsync("canon.enemy.pape-louis-xvii", "canon.boss.pape-louis-xvii", "Le Pape Louis XVII", "Le pape",
            new[] { "Antechamber", "Fear" }, "75", 4, 200, 24, 36, 12, 11,
            new[] { "canon.skill.brume", "canon.skill.flamme-froide", "skill.basic.strike" }, cancellationToken);

        // "Final" uniquement : Him'Lit est exclusivement le boss de la room qui recurre
        // tous les BossInterval (10) étages (cf. MarkovRoomTypeResolver) — il n'apparaît
        // pas comme rencontre normale dans les rooms Rupture/Forêt.
        await UpsertBossAsync("canon.enemy.himlit", "canon.boss.himlit", "Him'Lit", "Le maître des lieux, souverain du Palais",
            new[] { "Final" }, "100", 5, 280, 32, 48, 16, 13,
            new[] { "canon.skill.brume", "canon.skill.priere-aspiration", "canon.skill.flamme-seraphine", "canon.skill.flamme-froide", "skill.basic.strike" }, cancellationToken);
    }

    private async Task UpsertBossAsync(
        string enemyKey, string bossKey, string name, string description,
        string[] roomTypes, string danger, int difficulty,
        int vit, int atk, int def, int guard, int speed,
        string[] skillKeys, CancellationToken cancellationToken)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;

        // 1) l'EnemyDefinition + StatBlock du boss (compatible avec tous ses roomTypes)
        var enemy = await _ctx.EnemyDefinitions
            .Include(e => e.StatBlock).Include(e => e.SkillLinks)
            .FirstOrDefaultAsync(e => e.Key == enemyKey, cancellationToken);
        if (enemy is null)
        {
            enemy = new EnemyDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = enemyKey,
                Name = name,
                DisplayName = name,
                Description = description,
                NarrativeText = description,
                Version = version,
                Status = "Active",
                Archetype = "Boss",
                Family = "Canon",
                Rank = "Boss",
                Role = "Boss",
                BaseDifficulty = difficulty,
                EncounterWeight = 1,
                // Same 1-5 runtime scale as UpsertEnemyAsync's riskMin/riskMax (see the
                // comment above that method) — bosses are late-game, so they gate on the
                // top of the range rather than the raw 0-100 convention this used to use,
                // which made every RoomBoss/FinalBoss fight silently substitute a generic
                // enemy for the actual named boss.
                MinRiskLevel = 3,
                MaxRiskLevel = 5,
                MinDepth = 3,
                MaxDepth = 9,
                IsBoss = true,
                IsElite = true,
                BaseWeight = 1,
                CompatibleRoomTypesJson = JsonSerializer.Serialize(roomTypes),
                TagsJson = JsonSerializer.Serialize(new[] { "canon", "boss" }),
                SkillKeysJson = JsonSerializer.Serialize(skillKeys),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            enemy.StatBlock = new EnemyStatBlockEntity
            {
                Id = Guid.NewGuid(),
                EnemyDefinitionId = enemy.Id,
                MaxVitality = vit,
                AttackPower = atk,
                Defense = def,
                StartingGuard = guard,
                Speed = speed,
                Focus = difficulty * 2
            };
            foreach (var skillKey in skillKeys.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                enemy.SkillLinks.Add(new EnemySkillLinkEntity { EnemyDefinitionId = enemy.Id, SkillDefinitionKey = skillKey });
            }
            _ctx.EnemyDefinitions.Add(enemy);
        }
        else
        {
            enemy.Name = name; enemy.DisplayName = name;
            enemy.Description = description; enemy.NarrativeText = description;
            enemy.Version = version; enemy.Status = "Active";
            enemy.Archetype = "Boss"; enemy.Rank = "Boss"; enemy.Role = "Boss";
            enemy.BaseDifficulty = difficulty; enemy.IsBoss = true; enemy.IsElite = true;
            enemy.MinRiskLevel = 3; enemy.MaxRiskLevel = 5;
            enemy.CompatibleRoomTypesJson = JsonSerializer.Serialize(roomTypes);
            enemy.TagsJson = JsonSerializer.Serialize(new[] { "canon", "boss" });
            enemy.SkillKeysJson = JsonSerializer.Serialize(skillKeys);
            enemy.UpdatedAtUtc = now;
            enemy.StatBlock ??= new EnemyStatBlockEntity { Id = Guid.NewGuid(), EnemyDefinitionId = enemy.Id };
            enemy.StatBlock.MaxVitality = vit; enemy.StatBlock.AttackPower = atk;
            enemy.StatBlock.Defense = def; enemy.StatBlock.StartingGuard = guard;
            enemy.StatBlock.Speed = speed; enemy.StatBlock.Focus = difficulty * 2;

            var desired = skillKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var skillKey in desired.Where(k => enemy.SkillLinks.All(l => !string.Equals(l.SkillDefinitionKey, k, StringComparison.OrdinalIgnoreCase))))
            {
                enemy.SkillLinks.Add(new EnemySkillLinkEntity
                {
                    EnemyDefinitionId = enemy.Id,
                    SkillDefinitionKey = skillKey
                });
            }
        }

        // 2) une RoomBossDefinition par room type couvert. Le premier roomType garde la clé
        // d'origine (bossKey) pour rester rétrocompatible ; les suivants sont suffixés.
        for (var i = 0; i < roomTypes.Length; i++)
        {
            var roomType = roomTypes[i];
            var key = i == 0 ? bossKey : $"{bossKey}.{roomType.ToLowerInvariant()}";

            var boss = await _ctx.RoomBossDefinitions.FirstOrDefaultAsync(b => b.Key == key, cancellationToken);
            if (boss is null)
            {
                _ctx.RoomBossDefinitions.Add(new RoomBossDefinitionEntity
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    DisplayName = name,
                    Description = description,
                    RoomType = roomType,
                    EnemyDefinitionKey = enemyKey,
                    DangerHint = danger,
                    BaseDifficulty = difficulty,
                    BaseWeight = 1,
                    SelectionGroup = "boss.canon",
                    Version = version,
                    Status = "Active",
                    TagsJson = JsonSerializer.Serialize(new[] { "canon", "boss" }),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
                continue;
            }
            boss.DisplayName = name; boss.Description = description;
            boss.RoomType = roomType; boss.EnemyDefinitionKey = enemyKey; boss.DangerHint = danger;
            boss.BaseDifficulty = difficulty; boss.Version = version; boss.Status = "Active";
            boss.UpdatedAtUtc = now;
        }
    }

    // ── BUTIN CANON (tables de loot par ennemi + pool générique de secours) ────
    private async Task SeedCanonLootAsync(CancellationToken cancellationToken)
    {
        // enemyKey, entries (itemKey, dropPercent)
        await UpsertEnemyLootTableAsync("canon.enemy.lamiz",
            "Butin des Lamiz", "Ce que laisse une Lamiz vaincue.",
            new[]
            {
                new LootEntry("canon.item.dent-vorace", 45),
                new LootEntry("canon.item.lanterne", 25),
                new LootEntry("canon.item.filament-de-brume", 15),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.ombres-tentaculaires",
            "Butin des Ombres tentaculaires", "Ce que laisse une ombre tentaculaire vaincue.",
            new[]
            {
                new LootEntry("canon.item.filament-de-brume", 45),
                new LootEntry("canon.item.lanterne", 25),
                new LootEntry("canon.item.larme-de-racine", 15),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.fossoyeur-pale",
            "Butin du Fossoyeur pâle", "Ce que laisse un fossoyeur pâle vaincu.",
            new[]
            {
                new LootEntry("canon.item.poussiere-de-tombe", 50),
                new LootEntry("canon.item.dent-vorace", 25),
                new LootEntry("canon.item.filament-de-brume", 15),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.enfant-argile",
            "Butin de l'Enfant d'argile", "Ce que laisse un enfant d'argile vaincu.",
            new[]
            {
                new LootEntry("canon.item.sel-alchimique", 45),
                new LootEntry("canon.item.larme-de-racine", 25),
                new LootEntry("canon.item.poussiere-de-tombe", 20),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.voraces",
            "Butin des Voraces", "Ce que laisse un Vorace vaincu.",
            new[]
            {
                new LootEntry("canon.item.dent-vorace", 50),
                new LootEntry("canon.item.onguent-anxiete", 20),
                new LootEntry("canon.item.datura", 15),
                new LootEntry("canon.item.masque-bec-oiseau", 8),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.uguiro",
            "Butin d'Uguiro", "Ce que laisse Uguiro vaincu.",
            new[]
            {
                new LootEntry("canon.item.dent-vorace", 45),
                new LootEntry("canon.item.datura", 20),
                new LootEntry("canon.item.masque-bec-oiseau", 10),
                new LootEntry("canon.item.sel-alchimique", 8),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.oeil-du-visionnaire",
            "Butin de l'Œil du Visionnaire animé", "Ce que laisse l'Œil du Visionnaire animé vaincu.",
            new[]
            {
                new LootEntry("canon.item.filament-de-brume", 40),
                new LootEntry("canon.item.parchemin-cardinal", 20),
                new LootEntry("canon.item.datura", 15),
                new LootEntry("canon.item.oeil-visionnaire", 5),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.goule-anxiete",
            "Butin de la Goule", "Ce que laisse la Goule vaincue.",
            new[]
            {
                new LootEntry("canon.item.onguent-anxiete", 45),
                new LootEntry("canon.item.cendre-benite", 20),
                new LootEntry("canon.item.datura", 15),
                new LootEntry("canon.item.khamsa", 6),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.homoncule",
            "Butin de l'Homoncule", "Ce que laisse l'Homoncule vaincu.",
            new[]
            {
                new LootEntry("canon.item.sel-alchimique", 45),
                new LootEntry("canon.item.onguent-anxiete", 20),
                new LootEntry("canon.item.masque-bec-oiseau", 10),
                new LootEntry("canon.item.khamsa", 6),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.grand-cardinal",
            "Butin du Grand Cardinal", "Ce que laisse le Grand Cardinal vaincu.",
            new[]
            {
                new LootEntry("canon.item.parchemin-cardinal", 50),
                new LootEntry("canon.item.cendre-benite", 30),
                new LootEntry("canon.item.khamsa", 10),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.imperatrice-vipere",
            "Butin de l'Impératrice — la Vipère", "Ce que laisse l'Impératrice vaincue.",
            new[]
            {
                new LootEntry("canon.item.eclat-de-vipere", 40),
                new LootEntry("canon.item.onguent-anxiete", 25),
                new LootEntry("canon.item.khamsa", 12),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.homoncule-roi",
            "Butin de l'Homoncule — le Vieillard", "Ce que laisse le roi Homoncule vaincu.",
            new[]
            {
                new LootEntry("canon.item.sel-alchimique", 45),
                new LootEntry("canon.item.onguent-anxiete", 25),
                new LootEntry("canon.item.flamme-seraphine", 4),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.pape-louis-xvii",
            "Butin du Pape Louis XVII", "Ce que laisse le Pape vaincu.",
            new[]
            {
                new LootEntry("canon.item.parchemin-cardinal", 45),
                new LootEntry("canon.item.cendre-benite", 25),
                new LootEntry("canon.item.khamsa", 10),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.himlit",
            "Butin de Him'Lit", "Ce que laisse Him'Lit vaincu.",
            new[]
            {
                new LootEntry("canon.item.onguent-anxiete", 30),
                new LootEntry("canon.item.sel-alchimique", 25),
                new LootEntry("canon.item.ecaille-himlit", 3),
            }, cancellationToken);

        await UpsertGenericLootPoolAsync(
            "Trouvailles du Palais", "Ce que le Palais offre quand le butin d'un ennemi ne suffit pas.",
            new[]
            {
                new LootEntry("canon.item.lanterne", 55),
                new LootEntry("canon.item.cendre-benite", 35),
                new LootEntry("canon.item.poussiere-de-tombe", 25),
            }, cancellationToken);
    }

    private async Task UpsertEnemyLootTableAsync(
        string enemyDefinitionKey, string name, string description,
        IReadOnlyCollection<LootEntry> entries, CancellationToken cancellationToken)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var key = $"loot.enemy.{enemyDefinitionKey.Replace("canon.enemy.", string.Empty)}";

        var existing = await _ctx.EnemyLootTables.FirstOrDefaultAsync(t => t.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.EnemyLootTables.Add(new EnemyLootTableEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                EnemyDefinitionKey = enemyDefinitionKey,
                Name = name,
                Description = description,
                Version = version,
                Status = "Active",
                EntriesJson = JsonSerializer.Serialize(entries, J),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }

        existing.EnemyDefinitionKey = enemyDefinitionKey;
        existing.Name = name; existing.Description = description;
        existing.Version = version; existing.Status = "Active";
        existing.EntriesJson = JsonSerializer.Serialize(entries, J);
        existing.UpdatedAtUtc = now;
    }

    private async Task UpsertGenericLootPoolAsync(
        string name, string description,
        IReadOnlyCollection<LootEntry> entries, CancellationToken cancellationToken)
    {
        const string key = "loot.generic.fallback";
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;

        var existing = await _ctx.GenericLootPools.FirstOrDefaultAsync(p => p.Key == key, cancellationToken);
        if (existing is null)
        {
            _ctx.GenericLootPools.Add(new GenericLootPoolEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name,
                Description = description,
                Version = version,
                Status = "Active",
                EntriesJson = JsonSerializer.Serialize(entries, J),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }

        existing.Name = name; existing.Description = description;
        existing.Version = version; existing.Status = "Active";
        existing.EntriesJson = JsonSerializer.Serialize(entries, J);
        existing.UpdatedAtUtc = now;
    }
}