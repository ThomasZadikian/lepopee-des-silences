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
        await SeedHomonculeAsync(cancellationToken);
        await SeedEnfantAsync(cancellationToken);
        await SeedHimLitAsync(cancellationToken);
        await SeedTovmaAsync(cancellationToken);
        await SeedSathomAsync(cancellationToken);
        await SeedErinaAsync(cancellationToken);
        await SeedPomenianAsync(cancellationToken);
        await SeedCanonEnemiesAsync(cancellationToken);
        await SeedCanonSkillsAsync(cancellationToken);
        await SeedCanonItemsAsync(cancellationToken);
        await SeedCanonCursesAsync(cancellationToken);
        await SeedCanonLawsAsync(cancellationToken);
        await AttachCanonLawEffectsAsync(cancellationToken);
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

    // TODO(utilisateur) : npc.homoncule et npc.enfant existent tous les deux désormais et se
    // détestent mutuellement (confirmé narrativement), mais aucune convention de valeur pour
    // Weight n'a encore été établie (le champ n'est d'ailleurs consommé par aucune logique de
    // gameplay pour l'instant, juste transporté). Ne pas inventer un nombre ; câbler la paire
    // une fois la valeur confirmée.
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
                new NpcDialogueChoice("partir", "S'éloigner", Array.Empty<DialogueRequirement>(),
                    new[] { C(ConsequenceKind.Narrative, frag: "Vous le laissez à son seuil.") }, null)
            });

        var graph = new NpcDialogueGraph("npc.majordome.dialogue", "1.2", "seuil",
            new Dictionary<string, NpcDialogueNode> { ["seuil"] = seuil, ["confidence"] = confidence });

        // TODO(utilisateur) : liaison à une Room précise et offres concrètes non fournies
        // à ce stade — ne pas inventer, compléter une fois le contenu reçu.
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

        // TODO(utilisateur) : liaison à une Room précise et offres concrètes non fournies
        // à ce stade — ne pas inventer, compléter une fois le contenu reçu.
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

        // TODO(utilisateur) : liaison à une Room précise et offres concrètes non fournies
        // à ce stade — ne pas inventer, compléter une fois le contenu reçu.
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

        // TODO(utilisateur) : liaison à une Room précise et offres concrètes non fournies
        // à ce stade — ne pas inventer, compléter une fois le contenu reçu.
        var n = await UpsertNpcAsync("npc.owen", "Le Père Owen",
            "Le veilleur de la tour. Regard jaune et vitreux, prières nocturnes que nul ne devrait entendre.", "1.0",
            EmotionalRegister.Effroi, true, persona, wounds, graph, ct);
        n += await UpsertPoolAsync("pool.owen.benediction", "Owen — bénédiction", "Une grâce ambiguë, mais une grâce.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Reward, "Heal", null, 16) }, ct);
        n += await UpsertPoolAsync("pool.owen.malediction", "Owen — malédiction", "Le poids d'une foi reniée.", "1.0",
            new[] { new RewardCurseEntry(RewardCurseEntryKind.Curse, "GrantCurse", "curse.weight-of-silence") }, ct);
        return n;
    }

    // ── L'Homoncule (Rupture, première création du Forgeron) ─────────────────

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
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.enfant.craie") }, null)
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
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.tovma.lunettes") }, null)
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
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.sathom.bague") }, null)
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
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.erina.liberte") }, null)
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
                    new[] { C(ConsequenceKind.GrantOffering, offering: "offer.pomenian.connaissance") }, null)
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
            vitality: 22, attack: 6, defense: 3, guard: 0, speed: 13, focus: 3,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.voraces", "Voraces",
            "Hautes d'un mètre quarante à trois mètres, elles dévorent les énergies. Intelligentes, elles chassent en meute — ou seules, quand l'énergie est assez alléchante.",
            "Shadow", "Predateurs", "Elite", "Bruiser", isElite: true,
            depthMin: 2, depthMax: 8, riskMin: 25, riskMax: 80,
            roomTypes: new[] { "Rupture", "Fear", "Shadow" },
            tags: new[] { "canon", "predateur", "meute", "elite" },
            skillKeys: new[] { "skill.basic.strike" },
            vitality: 40, attack: 10, defense: 9, guard: 4, speed: 11, focus: 0,
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
            vitality: 48, attack: 12, defense: 12, guard: 5, speed: 8, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.ombres-tentaculaires", "Ombres tentaculaires",
            "Dans la brume, elles s'étirent jusqu'aux toits. On murmure des rats grands comme des chiens, des serpents à pattes — mais ce ne sont que ses bras.",
            "Shadow", "Brume", "Common", "Disruptor", isElite: false,
            depthMin: 1, depthMax: 5, riskMin: 1, riskMax: 45,
            roomTypes: new[] { "Threshold", "Fear" },
            tags: new[] { "canon", "ambiance", "brume" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 18, attack: 5, defense: 3, guard: 0, speed: 12, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.oeil-du-visionnaire", "L'Œil du Visionnaire animé",
            "Le symbole rampe sur les pavés au gré des flammes. Pupille en amande, violacée et jaune : il vous voit avant que vous ne le voyiez.",
            "Memory", "Lituisme", "Elite", "Disruptor", isElite: true,
            depthMin: 2, depthMax: 7, riskMin: 20, riskMax: 70,
            roomTypes: new[] { "Fear", "Memory" },
            tags: new[] { "canon", "lituisme", "surveillance", "motif" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 24, attack: 7, defense: 6, guard: 2, speed: 16, focus: 6,
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
            vitality: 38, attack: 9, defense: 6, guard: 3, speed: 12, focus: 3,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.homoncule", "L'Homoncule",
            "Né d'une flamme froide bleu-violet, nacré et soufré. Lent, presque doux — jusqu'à ce qu'il hurle. Le feu, le vrai, est sa seule terreur.",
            "Rupture", "Alchimie", "Elite", "Bruiser", isElite: true,
            depthMin: 3, depthMax: 9, riskMin: 40, riskMax: 95,
            roomTypes: new[] { "Rupture", "Memory" },
            tags: new[] { "canon", "alchimie", "homoncule", "elite", "weak.fire" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide", "canon.skill.priere-aspiration", "canon.skill.transmutation", "canon.skill.brume", "canon.skill.flamme-seraphine", "canon.skill.se-taire" },
            vitality: 52, attack: 13, defense: 15, guard: 6, speed: 7, focus: 0,
            cancellationToken);

        // ── Ennemis canon additionnels (renfort du bestiaire, mêmes familles/thèmes) ──
        await UpsertEnemyAsync(
            "canon.enemy.chien-de-priere", "Chien de prière",
            "Dressé par les prêtres pour flairer le doute. Il mord ceux qui hésitent au seuil.",
            "Shadow", "Lituisme", "Common", "Skirmisher", isElite: false,
            depthMin: 1, depthMax: 5, riskMin: 1, riskMax: 40,
            roomTypes: new[] { "Threshold", "Fear" },
            tags: new[] { "canon", "lituisme", "chien", "meute" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.flamme-froide" },
            vitality: 14, attack: 6, defense: 0, guard: 0, speed: 15, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.penitent-muet", "Le Pénitent muet",
            "Il a fait vœu de silence après avoir trop prié. Sa présence pèse, mais son corps refuse de céder.",
            "Silence", "Lituisme", "Common", "Guard", isElite: false,
            depthMin: 1, depthMax: 6, riskMin: 1, riskMax: 45,
            roomTypes: new[] { "Silence", "Threshold" },
            tags: new[] { "canon", "lituisme", "silence", "penitence" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.se-taire" },
            vitality: 24, attack: 5, defense: 9, guard: 4, speed: 8, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.racine-amere", "La Racine amère",
            "Une racine qui a bu trop de larmes. Elle se souvient à ta place — et t'en vole le prix.",
            "Forest", "Nature", "Common", "Support", isElite: false,
            depthMin: 1, depthMax: 6, riskMin: 1, riskMax: 40,
            roomTypes: new[] { "Forest", "Memory" },
            tags: new[] { "canon", "nature", "memoire", "racine" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.priere-aspiration" },
            vitality: 20, attack: 4, defense: 3, guard: 0, speed: 6, focus: 3,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.fossoyeur-pale", "Le Fossoyeur pâle",
            "Il creuse avant même que tu sois tombé. Rapide, silencieux, jamais las.",
            "Rupture", "Predateurs", "Common", "Skirmisher", isElite: false,
            depthMin: 2, depthMax: 7, riskMin: 10, riskMax: 55,
            roomTypes: new[] { "Rupture", "Threshold" },
            tags: new[] { "canon", "predateur", "fossoyeur" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.brume" },
            vitality: 18, attack: 7, defense: 3, guard: 0, speed: 14, focus: 0,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.enfant-argile", "L'Enfant d'argile",
            "Un essai raté de l'Homoncule, abandonné avant l'achèvement. Il soigne encore, par réflexe.",
            "Rupture", "Alchimie", "Common", "Support", isElite: false,
            depthMin: 2, depthMax: 6, riskMin: 5, riskMax: 45,
            roomTypes: new[] { "Rupture", "Memory" },
            tags: new[] { "canon", "alchimie", "argile", "enfant" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.transmutation" },
            vitality: 16, attack: 4, defense: 6, guard: 2, speed: 9, focus: 3,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.prieure-carmine", "La Prieure carmine",
            "Elle mène la prière quand les autres n'osent plus. Sa voix seule referme les blessures — et en rouvre d'autres.",
            "Shadow", "Lituisme", "Elite", "Support", isElite: true,
            depthMin: 3, depthMax: 8, riskMin: 30, riskMax: 80,
            roomTypes: new[] { "Fear", "Silence" },
            tags: new[] { "canon", "lituisme", "elite", "prieure" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.priere-aspiration", "canon.skill.se-taire", "canon.skill.flamme-seraphine" },
            vitality: 36, attack: 8, defense: 9, guard: 4, speed: 10, focus: 6,
            cancellationToken);

        await UpsertEnemyAsync(
            "canon.enemy.veilleur-ombre", "Le Veilleur d'ombre",
            "Il ne dort jamais, ne parle jamais. Il regarde, et ce qu'il regarde s'égare.",
            "Shadow", "Brume", "Elite", "Disruptor", isElite: true,
            depthMin: 3, depthMax: 9, riskMin: 30, riskMax: 85,
            roomTypes: new[] { "Rupture", "Shadow" },
            tags: new[] { "canon", "brume", "elite", "veilleur" },
            skillKeys: new[] { "skill.basic.strike", "canon.skill.brume", "canon.skill.flamme-froide" },
            vitality: 42, attack: 10, defense: 12, guard: 5, speed: 13, focus: 0,
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
        // et +8 de garde, tous deux répétés sur 5 tours. Le tick (2500) suit la même
        // convention que les autres effets périodiques canon (poison/regen) ; 5 tours =
        // 5 déclenchements, soit une durée de 5 * tickInterval.
        const int construcionPerpetuelleTickInterval = 2500;
        await UpsertSkillAsync("canon.skill.construction-perpetuelle", "Construction perpétuelle",
            "Ce que l'enfant a bâti continue de se construire, tour après tour, tant qu'on le laisse faire.",
            "Buff", "Self", "Buff", mana: 14, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("HealOverTime", null, 10, construcionPerpetuelleTickInterval * 5,
                    TickInterval: construcionPerpetuelleTickInterval, MagnitudeIsPercentOfMax: true),
                new SkillEffectSpec("GuardOverTime", null, 8, construcionPerpetuelleTickInterval * 5,
                    TickInterval: construcionPerpetuelleTickInterval)
            },
            category: "Magic");

        // "La liberté retrouvée" (Erina, sort légendaire) : frappe l'adversaire et
        // gagne +10% Vitesse (de base) pendant 10 tours. Même convention de tick
        // (2500/tour) que Construction perpétuelle ; l'effet est marqué AppliesToActor
        // car il doit revenir sur Erina/le lanceur, pas sur la cible frappée.
        const int liberteRetrouveeTicksPerTurn = 2500;
        await UpsertSkillAsync("canon.skill.liberte-retrouvee", "La liberté retrouvée",
            "Un coup porté comme une évasion — et pour un temps, plus rien ne la retient.",
            "Damage", "SingleEnemy", "Damage", mana: 20, power: 14, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, 10, liberteRetrouveeTicksPerTurn * 10,
                    Stat: "Speed", MagnitudeIsPercentOfBaseStat: true, AppliesToActor: true)
            },
            category: "Physical");

        // "Connaissance académique" (Pomenian, sort légendaire) : bénit toute l'équipe
        // de +10% dégâts des sorts (MagicDamageBonus) et -5% dégâts de sorts subis
        // (MagicDamageReduction). Cible AllAllies : chaque allié reçoit son propre
        // buff, donc pas d'AppliesToActor (la cible n'est déjà pas le lanceur seul).
        const int connaissanceAcademiqueTicksPerTurn = 2500;
        await UpsertSkillAsync("canon.skill.connaissance-academique", "Connaissance académique",
            "Un savoir cité comme on brandit une preuve — et pour un temps, l'équipe tout entière frappe et résiste comme s'il avait raison.",
            "Buff", "AllAllies", "Buff", mana: 22, power: 0, cancellationToken,
            effects: new[]
            {
                new SkillEffectSpec("StatModifier", null, 10, connaissanceAcademiqueTicksPerTurn * 5,
                    Stat: "MagicDamageBonus"),
                new SkillEffectSpec("StatModifier", null, 5, connaissanceAcademiqueTicksPerTurn * 5,
                    Stat: "MagicDamageReduction")
            },
            category: "Magic");
    }

    private async Task UpsertSkillAsync(
    string key, string name, string description,
    string skillType, string targeting, string effectType,
    int mana, int power, CancellationToken cancellationToken,
    IReadOnlyList<SkillEffectSpec>? effects = null,
    string category = "Physical")
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

        await UpsertItemAsync("canon.item.reve-erina", "Rêve d'Erina",
            "Un fragment de ce qu'elle imagine derrière chaque porte fermée. Tant qu'on le garde sur soi, on avance plus vite — comme elle.",
            "Relic", "Memento", "Rare", "RunOnly", false, 5, cancellationToken,
            effectRunType: "TeamSpeedBonus");

        await UpsertItemAsync("canon.item.monocle-pomenian", "Le monocle de Pomenian",
            "Une lentille gravée de formules alchimiques anciennes — celles-là mêmes que Pomenian refuse de considérer comme autre chose que des curiosités d'érudit. Quiconque le chausse voit, malgré lui, un peu plus loin que les livres.",
            "Equipment", "Accessory", "Epic", "Permanent", false, 0, cancellationToken,
            equipmentEffects: new[] { new ItemEquipmentEffect(ItemEquipmentEffectKind.MagicDamageBonusPercent, Amount: 10) });
    }

    private async Task UpsertItemAsync(
        string key, string name, string description,
        string category, string itemType, string rarity, string durability,
        bool usableInCombat, int effectValue, CancellationToken cancellationToken,
        IReadOnlyList<ItemEquipmentEffect>? equipmentEffects = null,
        bool isContainer = false, int? containerCapacity = null, bool isLiquid = false,
        string? effectRunType = null)
    {
        const string version = "canon-1.0.0";
        var now = DateTime.UtcNow;
        var lifecycle = durability == "Permanent" ? "PersistentMeta" : "RuntimeRunOnly";
        var duration = durability == "Permanent" ? "Permanent" : "RunOnly";
        var equipmentEffectsJson = JsonSerializer.Serialize(equipmentEffects ?? [], J);
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
        "room.hopital", "room.cellulehopital"
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
                MinRiskLevel = 30,
                MaxRiskLevel = 100,
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
        await UpsertEnemyLootTableAsync("canon.enemy.chimeres-serpentaires",
            "Butin des Chimères serpentaires", "Ce que laisse une chimère serpentaire vaincue.",
            new[]
            {
                new LootEntry("canon.item.datura", 30),
                new LootEntry("canon.item.cendre-benite", 40),
                new LootEntry("canon.item.lanterne", 20),
                new LootEntry("canon.item.khamsa", 6),
            }, cancellationToken);

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

        await UpsertEnemyLootTableAsync("canon.enemy.chien-de-priere",
            "Butin du Chien de prière", "Ce que laisse un chien de prière vaincu.",
            new[]
            {
                new LootEntry("canon.item.dent-vorace", 35),
                new LootEntry("canon.item.cendre-benite", 30),
                new LootEntry("canon.item.lanterne", 20),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.penitent-muet",
            "Butin du Pénitent muet", "Ce que laisse un pénitent muet vaincu.",
            new[]
            {
                new LootEntry("canon.item.cendre-benite", 45),
                new LootEntry("canon.item.parchemin-cardinal", 20),
                new LootEntry("canon.item.lanterne", 20),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.racine-amere",
            "Butin de la Racine amère", "Ce que laisse une racine amère vaincue.",
            new[]
            {
                new LootEntry("canon.item.larme-de-racine", 50),
                new LootEntry("canon.item.sel-alchimique", 20),
                new LootEntry("canon.item.lanterne", 15),
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

        await UpsertEnemyLootTableAsync("canon.enemy.prieure-carmine",
            "Butin de la Prieure carmine", "Ce que laisse la Prieure carmine vaincue.",
            new[]
            {
                new LootEntry("canon.item.cendre-benite", 40),
                new LootEntry("canon.item.onguent-anxiete", 25),
                new LootEntry("canon.item.khamsa", 8),
            }, cancellationToken);

        await UpsertEnemyLootTableAsync("canon.enemy.veilleur-ombre",
            "Butin du Veilleur d'ombre", "Ce que laisse le Veilleur d'ombre vaincu.",
            new[]
            {
                new LootEntry("canon.item.filament-de-brume", 45),
                new LootEntry("canon.item.masque-bec-oiseau", 20),
                new LootEntry("canon.item.datura", 12),
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