using Leds.Catalog.Application.Enemies.Definitions.Ports;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Enemies;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryEnemyDefinitionReadStore : IEnemyDefinitionReadStore
{
    private readonly IReadOnlyCollection<IEnemyDefinition> _definitions;

    public InMemoryEnemyDefinitionReadStore()
    {
        _definitions =
        [
            CreateDoubtFragment(),
            CreateInnerResistance(),
            CreateRootedRegret(),
            CreateWhisperingBranch(),
            CreateBrokenThought(),
            CreateContradiction(),
            CreateMuteWitness(),
            CreateAbsentVoice(),
            CreateArchivedWound(),
            CreateNamedLoss(),
            CreateDoorKeeper(),
            CreateLastRefusal(),
            CreateSilentDouble(),
            CreateLastEcho()
        ];
    }

    public Task<IReadOnlyCollection<IEnemyDefinition>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(d => d.IsActive)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IEnemyDefinition>>(definitions);
    }

    public Task<IEnemyDefinition?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var definition = _definitions.SingleOrDefault(d =>
            string.Equals(d.Key.Value, key, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(definition);
    }

    public Task<IReadOnlyCollection<IEnemyDefinition>> ListByRoomTypeAsync(
        string roomType,
        CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(d => d.IsActive)
            .Where(d => d.CompatibleRoomTypes.Any(rt =>
                string.Equals(rt, roomType, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IEnemyDefinition>>(definitions);
    }

    public Task<IReadOnlyCollection<IEnemyDefinition>> ListCompatibleAsync(
        string roomType,
        int riskLevel,
        CancellationToken cancellationToken)
    {
        var definitions = _definitions
            .Where(d => d.IsActive)
            .Where(d => d.MinRiskLevel <= riskLevel && riskLevel <= d.MaxRiskLevel)
            .Where(d => d.CompatibleRoomTypes.Any(rt =>
                string.Equals(rt, roomType, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IEnemyDefinition>>(definitions);
    }

    private static EnemyDefinition CreateDoubtFragment()
    {
        var def = EnemyDefinition.Create(
            "enemy.threshold.doubt-fragment",
            "Fragment de Doute",
            "Une hesitation cristallisee qui errait dans le seuil du Palais.",
            "1.0.0",
            "Fragile",
            baseDifficulty: 1,
            minRiskLevel: 1,
            maxRiskLevel: 2,
            compatibleRoomTypes: ["Threshold"],
            tags: ["threshold", "doubt", "fragile"],
            skillKeys: ["skill.basic.strike"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateInnerResistance()
    {
        var def = EnemyDefinition.Create(
            "enemy.threshold.inner-resistance",
            "Resistance Interieure",
            "Une volte-face tenace qui vous retient a l'entree du Palais.",
            "1.0.0",
            "Guard",
            baseDifficulty: 2,
            minRiskLevel: 2,
            maxRiskLevel: 3,
            compatibleRoomTypes: ["Threshold"],
            tags: ["threshold", "resistance", "guard"],
            skillKeys: ["skill.basic.guard"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateRootedRegret()
    {
        var def = EnemyDefinition.Create(
            "enemy.forest.rooted-regret",
            "Regret Enracine",
            "Un souvenir douloureux dont les racines plongent dans la terre du Palais.",
            "1.0.0",
            "Bruiser",
            baseDifficulty: 2,
            minRiskLevel: 1,
            maxRiskLevel: 3,
            compatibleRoomTypes: ["Forest"],
            tags: ["forest", "regret", "rooted"],
            skillKeys: ["skill.basic.strike"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateWhisperingBranch()
    {
        var def = EnemyDefinition.Create(
            "enemy.forest.whispering-branch",
            "Branche Murmurante",
            "Une ramure qui murmure des fragments de silences oublies.",
            "1.0.0",
            "Support",
            baseDifficulty: 2,
            minRiskLevel: 2,
            maxRiskLevel: 4,
            compatibleRoomTypes: ["Forest"],
            tags: ["forest", "whisper", "support"],
            skillKeys: ["skill.basic.weaken"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateBrokenThought()
    {
        var def = EnemyDefinition.Create(
            "enemy.rupture.broken-thought",
            "Pensee Brisee",
            "Une idee inachevee qui frappe par sa cacophonie.",
            "1.0.0",
            "Skirmisher",
            baseDifficulty: 3,
            minRiskLevel: 2,
            maxRiskLevel: 4,
            compatibleRoomTypes: ["Rupture"],
            tags: ["rupture", "fractured", "unstable"],
            skillKeys: ["skill.basic.strike"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateContradiction()
    {
        var def = EnemyDefinition.Create(
            "enemy.rupture.contradiction",
            "Contradiction",
            "Une negation pure qui desorganise toute tentative de progres.",
            "1.0.0",
            "Disruptor",
            baseDifficulty: 4,
            minRiskLevel: 3,
            maxRiskLevel: 5,
            compatibleRoomTypes: ["Rupture"],
            tags: ["rupture", "contradiction", "disruptor"],
            skillKeys: ["skill.basic.disrupt"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateMuteWitness()
    {
        var def = EnemyDefinition.Create(
            "enemy.silence.mute-witness",
            "Temoin Muet",
            "Une presence silencieuse qui observe sans jamais intervenir... jusqu'a ce qu'elle le fasse.",
            "1.0.0",
            "Guard",
            baseDifficulty: 3,
            minRiskLevel: 2,
            maxRiskLevel: 4,
            compatibleRoomTypes: ["Silence"],
            tags: ["silence", "witness", "guard"],
            skillKeys: ["skill.basic.guard"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateAbsentVoice()
    {
        var def = EnemyDefinition.Create(
            "enemy.silence.absent-voice",
            "Voix Absente",
            "Un vide laisse par une parole jamais prononcee. Il aspire la volonte.",
            "1.0.0",
            "Disruptor",
            baseDifficulty: 4,
            minRiskLevel: 3,
            maxRiskLevel: 5,
            compatibleRoomTypes: ["Silence"],
            tags: ["silence", "absence", "disruptor"],
            skillKeys: ["skill.basic.weaken"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateArchivedWound()
    {
        var def = EnemyDefinition.Create(
            "enemy.memory.archived-wound",
            "Blessure Archivee",
            "Une douleur conservee dans les archives du Palais, intacte et toujours vive.",
            "1.0.0",
            "Bruiser",
            baseDifficulty: 4,
            minRiskLevel: 2,
            maxRiskLevel: 5,
            compatibleRoomTypes: ["Memory"],
            tags: ["memory", "wound", "archive"],
            skillKeys: ["skill.basic.strike"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateNamedLoss()
    {
        var def = EnemyDefinition.Create(
            "enemy.memory.named-loss",
            "Perte Nommee",
            "Un nom grave dans la pierre du Palais, dont le poids ralentit tout voyageur.",
            "1.0.0",
            "Support",
            baseDifficulty: 4,
            minRiskLevel: 3,
            maxRiskLevel: 5,
            compatibleRoomTypes: ["Memory"],
            tags: ["memory", "loss", "support"],
            skillKeys: ["skill.basic.weaken"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateDoorKeeper()
    {
        var def = EnemyDefinition.Create(
            "enemy.antechamber.door-keeper",
            "Gardien de Porte",
            "La premiere des dernieres barrieres. Il ne laisse personne passer sans combat.",
            "1.0.0",
            "Guard",
            baseDifficulty: 5,
            minRiskLevel: 3,
            maxRiskLevel: 5,
            compatibleRoomTypes: ["Antechamber"],
            tags: ["antechamber", "gate", "guard"],
            skillKeys: ["skill.basic.guard"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateLastRefusal()
    {
        var def = EnemyDefinition.Create(
            "enemy.antechamber.last-refusal",
            "Dernier Refus",
            "L'ultime negation avant le Final. Un mur de volonte pure.",
            "1.0.0",
            "Bruiser",
            baseDifficulty: 5,
            minRiskLevel: 4,
            maxRiskLevel: 5,
            compatibleRoomTypes: ["Antechamber"],
            tags: ["antechamber", "refusal", "bruiser"],
            skillKeys: ["skill.basic.strike"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateSilentDouble()
    {
        var def = EnemyDefinition.Create(
            "enemy.final.silent-double",
            "Double Silencieux",
            "Votre reflet dans le miroir du Final. Il vous connait mieux que vous-meme.",
            "1.0.0",
            "Elite",
            baseDifficulty: 8,
            minRiskLevel: 4,
            maxRiskLevel: 5,
            compatibleRoomTypes: ["Final"],
            tags: ["final", "double", "elite"],
            skillKeys: ["skill.basic.strike", "skill.basic.disrupt"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }

    private static EnemyDefinition CreateLastEcho()
    {
        var def = EnemyDefinition.Create(
            "enemy.final.last-echo",
            "Dernier Echo",
            "Le dernier son avant le silence eternel. Resonant et implacable.",
            "1.0.0",
            "Elite",
            baseDifficulty: 9,
            minRiskLevel: 4,
            maxRiskLevel: 5,
            compatibleRoomTypes: ["Final"],
            tags: ["final", "echo", "elite"],
            skillKeys: ["skill.basic.weaken", "skill.basic.guard"],
            status: CatalogContentStatus.Draft);

        def.Activate();
        return def;
    }
}
