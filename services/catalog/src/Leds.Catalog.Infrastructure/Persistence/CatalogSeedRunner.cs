using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.Catalog.Domain.Npcs;
using Leds.Catalog.Domain.RewardCursePools;
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
        await SeedChapelierAsync(cancellationToken);
        await SeedElyasAsync(cancellationToken);
        await SeedOwenAsync(cancellationToken);
        await SeedCanonEnemiesAsync(cancellationToken);
        await SeedCanonSkillsAsync(cancellationToken);
        await SeedCanonItemsAsync(cancellationToken);
        await SeedCanonCursesAsync(cancellationToken);
        await SeedCanonLawsAsync(cancellationToken);
        await AttachCanonLawEffectsAsync(cancellationToken);
        await SeedCanonRoomsAsync(cancellationToken);
        await SeedCanonBossesAsync(cancellationToken);
        await SeedCanonRoomTypesAsync(cancellationToken);

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
        CancellationToken ct)
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
        e.UpdatedAtUtc = _now;

        if (existing is null)
        {
            _ctx.NpcDefinitions.Add(e);
        }

        return 1;
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
        int rel = 0, string? flag = null, string? wound = null) =>
        new(kind, when, frag, pool, null, rel, flag, wound, null, null, null);

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
                new NpcDialogueChoice("partir", "S'éloigner", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous le laissez à son seuil.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.majordome.dialogue", "1.2", "seuil",
            new Dictionary<string, NpcDialogueNode> { ["seuil"] = seuil, ["confidence"] = confidence });

        var n = await UpsertNpcAsync("npc.majordome", "Le Majordome",
            "Une présence du seuil : il accueille, il sert, il veille. Et il n'oublie rien.", "1.2",
            EmotionalRegister.Silence, true, persona, wounds, graph, ct);

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

        var n = await UpsertNpcAsync("npc.hitomi", "Hitomi",
            "Une présence douce, rencontrée sur un chemin de montagne. Son regard porte un vide ancien.", "1.0",
            EmotionalRegister.Memoire, true, persona, wounds, graph, ct);
        n += await UpsertPoolAsync("pool.hitomi.tendresse", "Hitomi — tendresse", "La chaleur d'une présence sincère.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", null, 20),
                    new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", null, 14) }, ct);
        n += await UpsertPoolAsync("pool.hitomi.retrait", "Hitomi — retrait", "Le froid d'une main qu'on n'attrape plus.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Curse, "Damage", null, 5) }, ct);
        return n;
    }

    // ── Le Chapelier (Déni) ──────────────────────────────────────────────────

    private async Task<int> SeedChapelierAsync(CancellationToken ct)
    {
        var persona = new NpcPersona("Fier, moderne, intarissable sur son ouvrage", EmotionalRegister.Deni,
            new[] { "la reconnaissance de son succès", "qu'on ne parle pas du deuil" },
            new[] { "un chapeau sur mesure", "du thé" });

        var wounds = new[]
        {
            new NpcWound("w-cendres", EmotionalRegister.Melancolie, NpcWoundReversibility.Irreversible, -2, -4,
                new[] { new NpcTransgression("w-cendres", "chapelier-cendres", -5) },
                "L'odeur de cendres revient. Il pleure ce qu'il refuse de nommer.")
        };

        var atelier = new NpcDialogueNode("atelier", "Le Chapelier",
            new[] { "Entrez ! Écoutez ces machines — le bruit même du succès.", "Un chapeau sur mesure, peut-être ? Je fais le meilleur ouvrage du pays." },
            new[]
            {
                new NpcDialogueChoice("commander", "Commander un chapeau", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Latent, pool: "pool.chapelier.ouvrage"),
                            C(ConsequenceKind.Narrative, when: WoundState.Rompu, frag: "Ses mains tremblent. Le feutre lui glisse des doigts."),
                            C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Rompu, pool: "pool.chapelier.cendres") }, null),
                new NpcDialogueChoice("parler-deuil", "Évoquer ce qu'il a perdu", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "chapelier-cendres"),
                            C(ConsequenceKind.Narrative, frag: "Son sourire se fige. Le vacarme des machines se déforme en un battement lourd.") }, "atelier"),
                new NpcDialogueChoice("complimenter", "Louer son ouvrage", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.Narrative, frag: "Il bombe le torse. « Enfin quelqu'un qui sait voir. »") }, null),
                new NpcDialogueChoice("partir", "Sortir de l'atelier", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Le clac-clac des machines vous suit jusque dans la rue.") }, null)
            },
            TenseLines: new[] { "Clac… clac… Vous entendez ce rythme ? Il… ralentit.", "Asseyez-vous. Ne regardez pas mes mains." },
            RupturedLines: new[] { "Vous sentez ? Cette odeur de cendres et de chair brûlée.", "Pourquoi ai-je l'impression d'avoir tué ce que j'avais de plus cher ?" });

        var graph = new NpcDialogueGraph("npc.chapelier.dialogue", "1.0", "atelier",
            new Dictionary<string, NpcDialogueNode> { ["atelier"] = atelier });

        var n = await UpsertNpcAsync("npc.chapelier", "Le Chapelier",
            "Maître artisan fier de sa modernité. Quelque chose, sous le vacarme des machines, refuse d'être nommé.", "1.0",
            EmotionalRegister.Deni, true, persona, wounds, graph, ct);
        n += await UpsertPoolAsync("pool.chapelier.ouvrage", "Chapelier — ouvrage", "La fierté d'un travail bien fait.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", null, 10) }, ct);
        n += await UpsertPoolAsync("pool.chapelier.cendres", "Chapelier — cendres", "Le deuil qui ne dit pas son nom.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Curse, "GrantCurse", "curse.weight-of-silence") }, ct);
        return n;
    }

    // ── Elyas (guide-prédécesseur, Mémoire) ──────────────────────────────────

    private async Task<int> SeedElyasAsync(CancellationToken ct)
    {
        var persona = new NpcPersona("Ancien, calme, énigmatique", EmotionalRegister.Memoire,
            new[] { "être entendu", "le respect du Palais" },
            new[] { "un savoir ancien", "une mise en garde" });

        var wounds = new[]
        {
            new NpcWound("w-oubli", EmotionalRegister.Memoire, NpcWoundReversibility.SoothableByScore, -1, -3,
                new[] { new NpcTransgression("w-oubli", "elyas-mepris", -4) },
                "Le Palais oublie vite ceux qui l'ont aimé. Lui s'en souvient pour deux.")
        };

        var seuilAncien = new NpcDialogueNode("seuil-ancien", "Elyas",
            new[] { "J'habitais ce Palais bien avant que tu n'en sois le concierge.", "Si tu es ici, c'est que tu as fui quelque chose. Veux-tu savoir ce que j'ai compris ?" },
            new[]
            {
                new NpcDialogueChoice("ecouter", "Écouter son savoir", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.Narrative, frag: "Il sourit, presque soulagé d'être enfin entendu.") }, "savoir"),
                new NpcDialogueChoice("exiger", "Exiger son pouvoir, sans écouter", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "elyas-mepris"),
                            C(ConsequenceKind.Narrative, frag: "« Le pouvoir ? Tu n'as donc rien écouté. »") }, "seuil-ancien"),
                new NpcDialogueChoice("partir", "Le laisser à ses souvenirs", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Tu t'éloignes. Il murmure un nom que tu ne connais pas.") }, null)
            },
            RupturedLines: new[] { "Tu me méprises, toi aussi. Comme tous les échos avant toi.", "Garde tes questions. Le Palais te les reprendra de toute façon." });

        var savoir = new NpcDialogueNode("savoir", "Elyas",
            new[] { "« Le Palais se nourrit du silence. Mais un savoir partagé devient une arme. »" },
            new[]
            {
                new NpcDialogueChoice("accepter-savoir", "Recevoir son enseignement", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Latent, pool: "pool.elyas.savoir"),
                            C(ConsequenceKind.Narrative, when: WoundState.Rompu, frag: "« Trop tard. Tu n'es plus digne d'écouter. »"),
                            C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Rompu, pool: "pool.elyas.rancune") }, null),
                new NpcDialogueChoice("refuser", "Décliner poliment", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.Narrative, frag: "« Sage. Tout savoir a un prix. »") }, null)
            });

        var graph = new NpcDialogueGraph("npc.elyas.dialogue", "1.0", "seuil-ancien",
            new Dictionary<string, NpcDialogueNode> { ["seuil-ancien"] = seuilAncien, ["savoir"] = savoir });

        var n = await UpsertNpcAsync("npc.elyas", "Elyas",
            "Un ancien habitant du Palais, là bien avant toi. Il t'appelle « le concierge ».", "1.0",
            EmotionalRegister.Memoire, true, persona, wounds, graph, ct);
        n += await UpsertPoolAsync("pool.elyas.savoir", "Elyas — savoir ancien", "Une vérité sur le Palais, gravée comme une loi.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Reward, "GrantLaw", "law.threshold.mefiance-des-echos") }, ct);
        n += await UpsertPoolAsync("pool.elyas.rancune", "Elyas — rancune", "Le ressentiment d'un écho oublié.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Curse, "GrantCurse", "curse.old-wound") }, ct);
        return n;
    }

    // ── Le Père Owen (Effroi, veilleur) ──────────────────────────────────────

    private async Task<int> SeedOwenAsync(CancellationToken ct)
    {
        var persona = new NpcPersona("Prêtre fou, doucereux, inquiétant", EmotionalRegister.Effroi,
            new[] { "réciter ses prières", "des oreilles pour l'écouter" },
            new[] { "une bénédiction impie", "un secret de l'abbaye" });

        var wounds = new[]
        {
            new NpcWound("w-foi", EmotionalRegister.Effroi, NpcWoundReversibility.Irreversible, -2, -4,
                new[] { new NpcTransgression("w-foi", "owen-blaspheme", -5) },
                "Tu as offensé le Seigneur du Lituisme. L'Œil ne se fermera plus.")
        };

        var abbaye = new NpcDialogueNode("abbaye", "Le Père Owen",
            new[] { "Approche, mon enfant. La nuit est le seul moment où l'on entend vraiment.", "Veux-tu une bénédiction ? Les miennes ne ressemblent à aucune autre." },
            new[]
            {
                new NpcDialogueChoice("benediction", "Recevoir sa bénédiction", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Latent, pool: "pool.owen.benediction"),
                            C(ConsequenceKind.Narrative, when: WoundState.Rompu, frag: "Il pose sa main froide sur ton front. Quelque chose s'y grave."),
                            C(ConsequenceKind.RewardOrCurseRoll, when: WoundState.Rompu, pool: "pool.owen.malediction") }, null),
                new NpcDialogueChoice("renier", "Renier le Lituisme devant lui", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.SetMemoryFlag, flag: "owen-blaspheme"),
                            C(ConsequenceKind.Narrative, frag: "Son regard jaune se fixe sur toi. Les bougies vacillent toutes en même temps.") }, "abbaye"),
                new NpcDialogueChoice("ecouter-priere", "Écouter sa prière", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.AdjustRelationship, rel: 1),
                            C(ConsequenceKind.Narrative, frag: "Les mots t'échappent, mais leur poids ne te lâche pas.") }, null),
                new NpcDialogueChoice("partir", "Quitter l'abbaye", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Derrière toi, le murmure des prières reprend, plus fort.") }, null)
            },
            TenseLines: new[] { "Tes pas hésitent. Le Seigneur n'aime pas l'hésitation.", "Reste dans la lumière des bougies, veux-tu." },
            RupturedLines: new[] { "Tu as renié le Seigneur devant moi. Il te voit, désormais.", "L'Œil du Visionnaire ne se ferme jamais. Plus jamais pour toi." });

        var graph = new NpcDialogueGraph("npc.owen.dialogue", "1.0", "abbaye",
            new Dictionary<string, NpcDialogueNode> { ["abbaye"] = abbaye });

        var n = await UpsertNpcAsync("npc.owen", "Le Père Owen",
            "Le veilleur de la tour. Regard jaune et vitreux, prières nocturnes que nul ne devrait entendre.", "1.0",
            EmotionalRegister.Effroi, true, persona, wounds, graph, ct);
        n += await UpsertPoolAsync("pool.owen.benediction", "Owen — bénédiction", "Une grâce ambiguë, mais une grâce.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", null, 16) }, ct);
        n += await UpsertPoolAsync("pool.owen.malediction", "Owen — malédiction", "Le poids d'une foi reniée.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Curse, "GrantCurse", "curse.weight-of-silence") }, ct);
        return n;
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
            "canon.enemy.chimeres-serpentaires", "Chimères serpentaires",
            "Elles rôdent les rues, la citadelle et les couloirs du Palais. À chaque prière prononcée, elles aspirent un peu de conscience. Elles sont en quête de quelque chose qu'elles ne nomment jamais.",
            "Shadow", "Lituisme", "Common", "Drain", isElite: false,
            depthMin: 1, depthMax: 6, riskMin: 1, riskMax: 50,
            roomTypes: new[] { "Threshold", "Fear", "Silence" },
            tags: new[] { "canon", "lituisme", "drain", "meute" },
                        skillKeys: new[] {
                "skill.basic.strike",
                "canon.skill.flamme-froide",
                "canon.skill.priere-aspiration",
                "canon.skill.transmutation",
                "canon.skill.brume",
                "canon.skill.flamme-seraphine",
                "canon.skill.se-taire"
            },
            vitality: 22, attack: 6, defense: 1, guard: 0, speed: 13, focus: 1,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.voraces", "Voraces",
            "Hautes d'un mètre quarante à trois mètres, elles dévorent les énergies. Intelligentes, elles chassent en meute — ou seules, quand l'énergie est assez alléchante.",
            "Shadow", "Predateurs", "Elite", "Bruiser", isElite: true,
            depthMin: 2, depthMax: 8, riskMin: 25, riskMax: 80,
            roomTypes: new[] { "Rupture", "Fear", "Shadow" },
            tags: new[] { "canon", "predateur", "meute", "elite" },
            skillKeys: new[] { "skill.basic.strike" },
            vitality: 40, attack: 10, defense: 3, guard: 4, speed: 11, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.lamiz", "Lamiz",
            "Une meute attirée par l'énergie « alléchante ». Là où l'une apparaît, les autres suivent.",
            "Shadow", "Predateurs", "Common", "Swarm", isElite: false,
            depthMin: 1, depthMax: 6, riskMin: 1, riskMax: 55,
            roomTypes: new[] { "Threshold", "Fear", "Shadow" },
            tags: new[] { "canon", "predateur", "meute" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 16, attack: 5, defense: 0, guard: 0, speed: 14, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.uguiro", "Uguiro",
            "Un monstre des profondeurs du Palais. Lent à se révéler, terrible une fois éveillé.",
            "Shadow", "Predateurs", "Elite", "Bruiser", isElite: true,
            depthMin: 3, depthMax: 9, riskMin: 35, riskMax: 90,
            roomTypes: new[] { "Rupture", "Shadow" },
            tags: new[] { "canon", "monstre", "elite" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 48, attack: 12, defense: 4, guard: 5, speed: 8, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.ombres-tentaculaires", "Ombres tentaculaires",
            "Dans la brume, elles s'étirent jusqu'aux toits. On murmure des rats grands comme des chiens, des serpents à pattes — mais ce ne sont que ses bras.",
            "Shadow", "Brume", "Common", "Disruptor", isElite: false,
            depthMin: 1, depthMax: 5, riskMin: 1, riskMax: 45,
            roomTypes: new[] { "Threshold", "Fear" },
            tags: new[] { "canon", "ambiance", "brume" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 18, attack: 5, defense: 1, guard: 0, speed: 12, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.oeil-du-visionnaire", "L'Œil du Visionnaire animé",
            "Le symbole rampe sur les pavés au gré des flammes. Pupille en amande, violacée et jaune : il vous voit avant que vous ne le voyiez.",
            "Memory", "Lituisme", "Elite", "Disruptor", isElite: true,
            depthMin: 2, depthMax: 7, riskMin: 20, riskMax: 70,
            roomTypes: new[] { "Fear", "Memory" },
            tags: new[] { "canon", "lituisme", "surveillance", "motif" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 24, attack: 7, defense: 2, guard: 2, speed: 16, focus: 2,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.goule-anxiete", "La Goule",
            "L'Anxiété personnifiée. Elle envahit, recouvre, étouffe — jusqu'au « Tais-toi » d'Elise qui, parfois, la fait reculer.",
            "Shadow", "Psyche", "Elite", "Drain", isElite: true,
            depthMin: 2, depthMax: 8, riskMin: 25, riskMax: 85,
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
            vitality: 38, attack: 9, defense: 2, guard: 3, speed: 12, focus: 1,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.homoncule", "L'Homoncule",
            "Né d'une flamme froide bleu-violet, nacré et soufré. Lent, presque doux — jusqu'à ce qu'il hurle. Le feu, le vrai, est sa seule terreur.",
            "Rupture", "Alchimie", "Elite", "Bruiser", isElite: true,
            depthMin: 3, depthMax: 9, riskMin: 40, riskMax: 95,
            roomTypes: new[] { "Rupture", "Memory" },
            tags: new[] { "canon", "alchimie", "homoncule", "elite", "weak.fire" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 52, attack: 13, defense: 5, guard: 6, speed: 7, focus: 0,
            cancellationToken);
    }

    private async Task UpsertEnemyAsync(
        string key, string name, string description,
        string archetype, string family, string rank, string role, bool isElite,
        int depthMin, int depthMax, int riskMin, int riskMax,
        string[] roomTypes, string[] tags, string[] skillKeys,
        int vitality, int attack, int defense, int guard, int speed, int focus,
        CancellationToken cancellationToken)
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
                Initiative = 0,
                Recovery = 0,
                Focus = focus,
                Mana = 0,
                Charge = 0
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
        await UpsertSkillAsync("canon.skill.flamme-froide", "Flamme froide",
            "Bleu-violet, elle ne brûle pas la peau mais la chair, et le givre transperce l'os. Le sort de l'apothicaire.",
            "Damage", "SingleEnemy", "Damage", mana: 8, power: 22, cancellationToken);

        await UpsertSkillAsync("canon.skill.priere-aspiration", "Prière",
            "Une prière lituique aspire la conscience. Elle restaure — mais nourrit ce qui rôde, et gonfle l'Égo.",
            "Drain", "SingleEnemy", "Debuff", mana: 4, power: 12, cancellationToken);

        await UpsertSkillAsync("canon.skill.transmutation", "Transmutation",
            "Plomb, or, mercure, soufre, sel. L'art alchimique réordonne la matière de l'instant.",
            "Buff", "Self", "Buff", mana: 6, power: 0, cancellationToken);

        await UpsertSkillAsync("canon.skill.brume", "Brume",
            "Le brouillard non-naturel se lève. Portée et précision s'effondrent — pour tous.",
            "Debuff", "AllEnemies", "Debuff", mana: 7, power: 0, cancellationToken);

        await UpsertSkillAsync("canon.skill.flamme-seraphine", "Flamme Séraphine",
            "Le feu, le vrai. La seule terreur de l'Homoncule. Pure, dévorante, sans gel.",
            "Damage", "SingleEnemy", "Damage", mana: 12, power: 34, cancellationToken);

        await UpsertSkillAsync("canon.skill.se-taire", "Se taire",
            "Ne rien dire. Ne pas prier. L'acte de silence. Inutile contre la chair — dévastateur contre ce qui se nourrit de la voix.",
            "Silence", "Self", "Status", mana: 0, power: 0, cancellationToken,
            effectKind: "Silence",
            effectDurationTicks: 3);
    }

    private async Task UpsertSkillAsync(
    string key, string name, string description,
    string skillType, string targeting, string effectType,
    int mana, int power, CancellationToken cancellationToken,
    string? effectKind = null, string? effectStatusKey = null,
    int effectMagnitude = 0, int effectDurationTicks = 0,
    int effectTickInterval = 0, string? effectStat = null)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
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
                CostType = mana > 0 ? "Mana" : "None",
                ManaCost = mana,
                ChargeCost = 0,
                BasePower = power,
                Power = power,
                Accuracy = 100,
                ActionCost = 10,
                BaseWeight = 1,
                EffectKind = effectKind,
                EffectStatusKey = effectStatusKey,
                EffectMagnitude = effectMagnitude,
                EffectDurationTicks = effectDurationTicks,
                EffectTickInterval = effectTickInterval,
                EffectStat = effectStat,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }
        existing.Name = name; existing.DisplayName = name;
        existing.Description = description; existing.NarrativeText = description;
        existing.Version = version; existing.Status = "Active";
        existing.SkillType = skillType; existing.TargetingType = targeting; existing.TargetingMode = targeting;
        existing.EffectType = effectType; existing.CostType = mana > 0 ? "Mana" : "None";
        existing.ManaCost = mana; existing.BasePower = power; existing.Power = power;
        existing.EffectKind = effectKind;
        existing.EffectStatusKey = effectStatusKey;
        existing.EffectMagnitude = effectMagnitude;
        existing.EffectDurationTicks = effectDurationTicks;
        existing.EffectTickInterval = effectTickInterval;
        existing.EffectStat = effectStat;
        existing.UpdatedAtUtc = now;
    }

    // ── OBJETS CANON ──────────────────────────────────────────────────────────
    private async Task SeedCanonItemsAsync(CancellationToken cancellationToken)
    {
        // key, name, desc, category, itemType, rarity, lifecycle, usableInCombat, effectValue
        await UpsertItemAsync("canon.item.tome-38", "Le Tome 38",
            "« L'épopée du Silence ». Les notes du 38ᵉ écho, reliées dans une peau humaine. Celui qui le lit n'est jamais tout à fait seul.",
            "Relic", "Lore", "Unique", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.carnet-pomenian", "Le carnet de Pomenian",
            "Des observations méthodiques, une écriture qui se dégrade page après page. Des vérités que l'auteur aurait dû taire.",
            "Relic", "Lore", "Rare", "Permanent", false, 0, cancellationToken);

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
            "Key", "Container", "Epic", "Permanent", false, 0, cancellationToken);

        await UpsertItemAsync("canon.item.flamme-seraphine", "La Flamme Séraphine",
            "Une flamme à recueillir, jamais à posséder. Elle accorde le seul feu qui fasse hurler l'Homoncule.",
            "Relic", "Flame", "Legendary", "Permanent", true, 0, cancellationToken);
    }

    private async Task UpsertItemAsync(
        string key, string name, string description,
        string category, string itemType, string rarity, string durability,
        bool usableInCombat, int effectValue, CancellationToken cancellationToken)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var lifecycle = durability == "Permanent" ? "PersistentMeta" : "RuntimeRunOnly";
        var duration = durability == "Permanent" ? "Permanent" : "RunOnly";
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
        await UpsertLawEffectAsync("canon.law.arrete-153-2", "ModifySpeed", -2m, "UntilRunEnds", null, cancellationToken);
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
        int depthMin, int depthMax, CancellationToken cancellationToken)
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
                IsCulturalEcho = true,
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
        existing.Version = version; existing.Status = "Active"; existing.UpdatedAtUtc = now;
    }
    // ── BOSS CANON ────────────────────────────────────────────────────────────
    // Crée à la fois l'EnemyDefinition (IsBoss) + StatBlock + le RoomBossDefinition.
    private async Task SeedCanonBossesAsync(CancellationToken cancellationToken)
    {
        // enemyKey, bossKey, name, desc, roomType, danger, difficulty, vit, atk, def, guard, speed, skillKey
        await UpsertBossAsync(
            "canon.enemy.grand-cardinal",
            "canon.boss.grand-cardinal",
            "Le Grand Cardinal",
            "Le grand cardinal du Palais",
            "Antechamber",
            "75",
            2, 90, 14, 6, 6, 12,
            new[]
                { "canon.skill.priere-aspiration", "canon.skill.flamme-froide", "skill.basic.strike" },
            cancellationToken);

        await UpsertBossAsync("canon.enemy.imperatrice-vipere", "canon.boss.imperatrice-vipere", "L'Impératrice — la Vipère", "L'impératrice du Palais",
            "Rupture", "75", 3, 140, 20, 8, 8, 14,
            new[] { "canon.skill.priere-aspiration", "canon.skill.flamme-froide", "skill.basic.strike" }, cancellationToken);

        await UpsertBossAsync("canon.enemy.homoncule-roi", "canon.boss.homoncule-roi", "L'Homoncule — le Vieillard", "Le roi, l'Homoncule, bien des nom lui furent donné",
            "Rupture", "75", 3, 160, 22, 9, 10, 8,
            new[] { "canon.skill.transmutation", "canon.skill.flamme-froide", "skill.basic.strike" }, cancellationToken);

        await UpsertBossAsync("canon.enemy.pape-louis-xvii", "canon.boss.pape-louis-xvii", "Le Pape Louis XVII", "Le pape",
            "Antechamber", "75", 4, 200, 24, 12, 12, 11,
            new[] { "canon.skill.brume", "canon.skill.flamme-froide", "skill.basic.strike" }, cancellationToken);

        await UpsertBossAsync("canon.enemy.himlit", "canon.boss.himlit", "Him'Lit", "Le maître des lieux, souverain du Palais",
            "Rupture", "100", 5, 280, 32, 16, 16, 13,
            new[] { "canon.skill.brume", "canon.skill.priere-aspiration", "canon.skill.flamme-seraphine", "canon.skill.flamme-froide", "skill.basic.strike" }, cancellationToken);
    }

    private async Task UpsertBossAsync(
        string enemyKey, string bossKey, string name, string description,
        string roomType, string danger, int difficulty,
        int vit, int atk, int def, int guard, int speed,
        string[] skillKeys, CancellationToken cancellationToken)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;

        // 1) l'EnemyDefinition + StatBlock du boss
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
                MinRiskLevel = 30,
                MaxRiskLevel = 100,
                MinDepth = 3,
                MaxDepth = 9,
                IsBoss = true,
                IsElite = true,
                BaseWeight = 1,
                CompatibleRoomTypesJson = JsonSerializer.Serialize(new[] { roomType }),
                TagsJson = JsonSerializer.Serialize(new[] { "canon", "boss" }),
                SkillKeysJson = JsonSerializer.Serialize(new[] { "skill.basic.strike" }),
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
                Focus = 2
            };
            enemy.SkillLinks.Add(new EnemySkillLinkEntity { EnemyDefinitionId = enemy.Id, SkillDefinitionKey = "skill.basic.strike" });
            _ctx.EnemyDefinitions.Add(enemy);
        }
        else
        {
            enemy.Name = name; enemy.DisplayName = name;
            enemy.Description = description; enemy.NarrativeText = description;
            enemy.Version = version; enemy.Status = "Active";
            enemy.Archetype = "Boss"; enemy.Rank = "Boss"; enemy.Role = "Boss";
            enemy.BaseDifficulty = difficulty; enemy.IsBoss = true; enemy.IsElite = true;
            enemy.CompatibleRoomTypesJson = JsonSerializer.Serialize(new[] { roomType });
            enemy.UpdatedAtUtc = now;
            enemy.StatBlock ??= new EnemyStatBlockEntity { Id = Guid.NewGuid(), EnemyDefinitionId = enemy.Id };
            enemy.StatBlock.MaxVitality = vit; enemy.StatBlock.AttackPower = atk;
            enemy.StatBlock.Defense = def; enemy.StatBlock.StartingGuard = guard;
            enemy.StatBlock.Speed = speed; enemy.StatBlock.Focus = 2;
        }

        // 2) le RoomBossDefinition qui pointe vers cet ennemi
        var boss = await _ctx.RoomBossDefinitions.FirstOrDefaultAsync(b => b.Key == bossKey, cancellationToken);
        if (boss is null)
        {
            _ctx.RoomBossDefinitions.Add(new RoomBossDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Key = bossKey,
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
            return;
        }
        boss.DisplayName = name; boss.Description = description;
        boss.RoomType = roomType; boss.EnemyDefinitionKey = enemyKey; boss.DangerHint = danger;
        boss.BaseDifficulty = difficulty; boss.Version = version; boss.Status = "Active";
        boss.UpdatedAtUtc = now;
    }
}