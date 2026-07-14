using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Chimères des Plaines" — scripts déterministes pour les
/// trois créatures. La mécanique de famille "La Faim" (compteur partagé entre
/// toutes les Chimères d'un combat, chargé par les DoT adverses, critique garanti
/// à 5 crans) n'est pas modélisée — nécessiterait un compteur partagé au niveau du
/// Combat, absent du moteur. Approximée ici par un ciblage préférentiel des
/// adversaires déjà lourdement affectés par des DoT, sans compteur ni garantie de
/// critique. Voir CatalogSeedRunner.SeedBestiaireChimeresDesPlainesAsync pour le
/// détail des autres approximations (lifesteal de Curée, intargetabilité de Bond
/// de flanc, esquive de Guet, déclenchement à la mort de Détente).
/// </summary>
public sealed class ChimereAffameeBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.chimere-affamee";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        var weakest = LowestHpPlayer(combat, boss);
        if (weakest is not null && HpFraction(weakest) < 0.40)
            return Strike(boss, "canon.skill.curee", weakest);

        var mostDotted = LivingPlayers(combat)
            .Where(p => p.StatusEffects.Count(e => e.Kind == StatusEffectKind.DamageOverTime) >= 2)
            .OrderByDescending(p => p.StatusEffects.Count(e => e.Kind == StatusEffectKind.DamageOverTime))
            .ThenBy(p => DeterministicTieKey(combat, boss, p))
            .FirstOrDefault();
        if (mostDotted is not null)
            return Strike(boss, "canon.skill.morsure-composite", mostDotted);

        var noOneIsDotted = LivingPlayers(combat)
            .All(p => p.StatusEffects.All(e => e.Kind != StatusEffectKind.DamageOverTime));
        if (noOneIsDotted)
            return Chance(combat, boss, "guet", 0.40)
                ? OnSelf(boss, "canon.skill.guet")
                : Strike(boss, "canon.skill.bond-de-flanc", weakest);

        return Strike(boss, "canon.skill.morsure-composite", weakest)
            ?? Strike(boss, "canon.skill.bond-de-flanc", weakest);
    }
}

/// <summary>
/// Le Berger d'Ordres — cerveau tactique de la meute. Désigne au premier tour,
/// nourrit un allié blessé, sinon maudit une cible saine ou martèle au corps-à-
/// corps.
/// </summary>
public sealed class BergerOrdresBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.berger-ordres";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        if (combat.TurnNumber == 1)
            return Strike(boss, "canon.skill.designation", LowestHpPlayer(combat, boss));

        var hasWoundedAlly = combat.Enemies
            .Where(e => !e.IsDefeated && e.Id.Value != boss.Id.Value)
            .Any(e => HpFraction(e) < 0.50);
        if (hasWoundedAlly)
            return OnAllAllies(boss, "canon.skill.ration", combat);

        var undotted = PlayerWithoutDot(combat, boss);
        return undotted is not null
            ? Strike(boss, "canon.skill.plongee-dans-la-folie", undotted)
            : Strike(boss, "canon.skill.houlette", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// L'Agneau Inversé — piège pédagogique. Broute deux tours, puis alterne un
/// marquage et des dégâts magiques ; se détend volontairement sous 25% PV face à
/// plusieurs adversaires.
/// </summary>
public sealed class AgneauInverseBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.agneau-inverse";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        if (HpFraction(boss) < 0.25 && LivingPlayers(combat).Count >= 2)
            return OnAllPlayers(boss, "canon.skill.detente", combat);

        if (combat.TurnNumber <= 2)
            return OnSelf(boss, "canon.skill.brout");

        var target = FastestPlayer(combat, boss);
        var alreadyMarked = target is not null && target.StatusEffects.Any(e =>
            string.Equals(e.Key, "canon.skill.regard-fixe:StatModifier", StringComparison.OrdinalIgnoreCase));

        if (!alreadyMarked)
            return Strike(boss, "canon.skill.regard-fixe", target);

        return Strike(boss, "canon.skill.belement-a-lenvers", target)
            ?? Strike(boss, "canon.skill.regard-fixe", target);
    }
}
