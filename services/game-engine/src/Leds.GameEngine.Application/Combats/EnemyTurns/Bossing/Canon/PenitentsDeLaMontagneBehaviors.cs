using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Pénitents de la Montagne" — scripts déterministes pour
/// les trois créatures. La mécanique de famille "Le Pèlerinage" (Stations
/// partagées, manifestation de la Frayeur Exhumée en renfort) n'est pas
/// modélisée — voir le commentaire dans
/// CatalogSeedRunner.SeedBestiairePenitentsDeLaMontagneAsync.
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée pour
/// les 3 créatures (Chantier Bestiaire Phase 13). Le Pèlerin Sans Visage réutilise
/// son propre buff sur soi (Repentir) déjà présent dans son kit de 4 ; le Prieur
/// Lituique et la Frayeur Exhumée, sans alternative défensive, ripostent contre
/// leur dernier agresseur (<see cref="CanonBossBehaviorBase.LastAttacker"/>) avec
/// leur sort le plus offensif à la place.
/// </para>
/// </summary>
public sealed class PelerinSansVisageBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.pelerin-sans-visage";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : la faute oubliée
        // exige quand même son prix (Repentir, sur soi). Prend priorité sur le
        // plan normal, y compris le seuil de PV bas ci-dessous.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.repentir")
                ?? Strike(boss, "canon.skill.chapelet-de-dents", LowestHpPlayer(combat, boss));

        if (HpFraction(boss) < 0.50)
            return OnSelf(boss, "canon.skill.repentir")
                ?? Strike(boss, "canon.skill.chapelet-de-dents", LowestHpPlayer(combat, boss));

        if (combat.TurnNumber <= 2)
            return Strike(boss, "canon.skill.priere-aspiration", HighestHpPlayer(combat, boss))
                ?? Strike(boss, "canon.skill.baton-de-marche", LowestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.chapelet-de-dents", LowestMagicDefensePlayer(combat, boss))
            ?? Strike(boss, "canon.skill.baton-de-marche", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// Le Prieur Lituique — élite support/drain. Ouvre par Encens inversé, draine la
/// cible déjà sous -DEF, presse le rythme en fin de combat.
/// </summary>
public sealed class PrieurLituiqueBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.prieur-lituique";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : aucun sort
        // défensif dans son kit, riposte contre son dernier agresseur avec son
        // sort le plus offensif. Prend priorité sur le plan normal ; si aucun
        // agresseur vivant n'est identifiable, continue sur le plan normal.
        if (TookPowerfulHit(boss))
        {
            var retaliation = Strike(boss, "canon.skill.derniere-priere", LastAttacker(combat, boss))
                ?? Strike(boss, "canon.skill.oraison-cousue", LastAttacker(combat, boss));
            if (retaliation is not null) return retaliation;
        }

        if (combat.TurnNumber == 1)
            return OnAllPlayers(boss, "canon.skill.encens-inverse", combat)
                ?? Strike(boss, "canon.skill.priere-aspiration", HighestHpPlayer(combat, boss));

        if (combat.TurnNumber >= 6)
            return Strike(boss, "canon.skill.derniere-priere", HighestHpPlayer(combat, boss))
                ?? Strike(boss, "canon.skill.priere-aspiration", HighestHpPlayer(combat, boss));

        var drained = LivingPlayers(combat).FirstOrDefault(p => HasNegativeStatModifier(p, CombatStat.Defense));
        if (drained is not null)
            return Strike(boss, "canon.skill.oraison-cousue", drained)
                ?? Strike(boss, "canon.skill.priere-aspiration", HighestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.priere-aspiration", HighestHpPlayer(combat, boss));
    }
}

/// <summary>
/// La Frayeur Exhumée — mini-élite invocable. Marque la cible au Focus le plus
/// élevé à son arrivée, puis alterne peur organique et névrose.
/// </summary>
public sealed class FrayeurExhumeeBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.frayeur-exhumee";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : aucun sort
        // défensif dans son kit, riposte contre son dernier agresseur avec son
        // sort le plus offensif. Prend priorité sur le plan normal ; si aucun
        // agresseur vivant n'est identifiable, continue sur le plan normal.
        if (TookPowerfulHit(boss))
        {
            var retaliation = Strike(boss, "canon.skill.griffe-de-recul", LastAttacker(combat, boss))
                ?? Strike(boss, "canon.skill.frayeur-organique", LastAttacker(combat, boss));
            if (retaliation is not null) return retaliation;
        }

        if (combat.TurnNumber == 1)
        {
            var mostFocused = LivingPlayers(combat)
                .OrderByDescending(p => p.EffectiveFocus)
                .FirstOrDefault();
            return Strike(boss, "canon.skill.posture-finale", mostFocused)
                ?? Strike(boss, "canon.skill.frayeur-organique", LowestHpPlayer(combat, boss));
        }

        var undotted = PlayerWithoutDot(combat, boss);
        return Strike(boss, "canon.skill.nevrose", undotted ?? LowestHpPlayer(combat, boss))
            ?? Strike(boss, "canon.skill.frayeur-organique", LowestHpPlayer(combat, boss));
    }
}
