using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Créations du Forgeron" — scripts déterministes pour
/// les quatre créatures. La mécanique de famille "La Trempe" (+2 DEF quand une
/// Création reçoit un buff d'Attaque) est câblée directement dans les sorts
/// (voir CatalogSeedRunner.SeedBestiaireCreationsDuForgeronAsync), pas ici.
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée pour
/// les 4 créatures (Chantier Bestiaire Phase 13), chacune via le sort défensif/de
/// soin déjà présent dans son kit de 4 : Création Instable → Égide, Sentinelle de
/// Fonte → Fonte, Scorie Rampante → Reformation. Le Marteau Vivant, un DPS pur
/// sans alternative défensive, riposte contre son dernier agresseur
/// (<see cref="CanonBossBehaviorBase.LastAttacker"/>) avec son sort le plus
/// offensif à la place. Cette réaction prend priorité sur la logique existante de
/// riposte "Foyer ouvert" de la Création Instable (déclenchée à chaque tour où
/// elle a un agresseur connu, sans seuil de dégâts) — les deux coexistent, le
/// coup très puissant passant simplement en premier.
/// </para>
/// </summary>
public sealed class CreationInstableBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.creation-instable";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : lève une égide
        // mentale. Prend priorité sur le plan normal, y compris "Foyer ouvert".
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.egide");

        if (boss.LastAttackerId is { } attackerId)
        {
            var attacker = LivingPlayers(combat).FirstOrDefault(p => p.Id.Value == attackerId);
            if (attacker is not null && Chance(combat, boss, "foyer-ouvert", 0.65))
                return Strike(boss, "canon.skill.foyer-ouvert", attacker);
        }

        if (HpFraction(boss) < 0.50 && Chance(combat, boss, "egide", 0.80))
            return OnSelf(boss, "canon.skill.egide");

        return Chance(combat, boss, "cycle", 0.50)
            ? Strike(boss, "canon.skill.coup-de-plaque", LowestHpPlayer(combat, boss))
            : OnSelf(boss, "canon.skill.redressement");
    }
}

/// <summary>
/// Le Marteau Vivant — DPS physique de la Forge. Enchaîne Souffle dès qu'un buff
/// d'Attaque est actif sur lui (le combo signature avec la Sentinelle de Fonte),
/// achève les cibles déjà sous DoT, sinon martèle en rythme.
/// </summary>
public sealed class MarteauVivantBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.marteau-vivant";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : aucun sort
        // défensif dans son kit (DPS pur), riposte contre son dernier
        // agresseur avec son sort le plus offensif. Prend priorité sur le plan
        // normal ; si aucun agresseur vivant n'est identifiable, continue.
        if (TookPowerfulHit(boss))
        {
            var retaliation = Strike(boss, "canon.skill.coup-de-grace-forgeron", LastAttacker(combat, boss))
                ?? Strike(boss, "canon.skill.frappe-denclume", LastAttacker(combat, boss));
            if (retaliation is not null) return retaliation;
        }

        var isAttackBuffed = boss.StatusEffects.Any(e =>
            e.Kind == StatusEffectKind.StatModifier && e.Stat == CombatStat.AttackPower && e.Magnitude > 0);
        if (isAttackBuffed)
            return Strike(boss, "canon.skill.souffle-de-la-forge", LowestHpPlayer(combat, boss));

        var dottedTarget = LivingPlayers(combat)
            .FirstOrDefault(p => p.StatusEffects.Any(e => e.Kind == StatusEffectKind.DamageOverTime));
        if (dottedTarget is not null)
            return Strike(boss, "canon.skill.coup-de-grace-forgeron", dottedTarget)
                ?? Strike(boss, "canon.skill.frappe-denclume", dottedTarget);

        if (combat.TurnNumber == 1)
            return OnSelf(boss, "canon.skill.cadence");

        return Strike(boss, "canon.skill.frappe-denclume", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// La Sentinelle de Fonte — activaterice du combo de la Forge. Transmute le
/// Marteau Vivant au premier tour, récite en groupe, sinon travaille la cible la
/// moins défendue.
/// </summary>
public sealed class SentinelleFonteBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.sentinelle-fonte";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : Fonte. Prend
        // priorité sur le plan normal.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.fonte");

        var marteau = combat.Enemies.FirstOrDefault(e =>
            !e.IsDefeated && string.Equals(e.SourceKey, "canon.enemy.marteau-vivant", StringComparison.OrdinalIgnoreCase));
        if (combat.TurnNumber == 1 && marteau is not null)
            return Strike(boss, "canon.skill.transmutation-alliee", marteau);

        var livingAllies = combat.Enemies.Count(e => !e.IsDefeated);
        if (livingAllies >= 3 && combat.TurnNumber % 3 == 0)
            return OnAllAllies(boss, "canon.skill.litanie", combat);

        if (HpFraction(boss) < 0.50)
            return OnSelf(boss, "canon.skill.fonte")
                ?? Strike(boss, "canon.skill.scorie", LeastDefendedPlayer(combat, boss));

        return Strike(boss, "canon.skill.scorie", LeastDefendedPlayer(combat, boss));
    }
}

/// <summary>
/// La Scorie Rampante — chair à canon de la Forge, entretient les Brûlures.
/// </summary>
public sealed class ScorieRampanteBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.scorie-rampante";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : se reforme.
        // Prend priorité sur le plan normal.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.reformation");

        var dottedTarget = LivingPlayers(combat)
            .FirstOrDefault(p => p.StatusEffects.Any(e => e.Kind == StatusEffectKind.DamageOverTime));
        if (dottedTarget is not null && Chance(combat, boss, "laitier-ardent", 0.75))
            return Strike(boss, "canon.skill.laitier-ardent", dottedTarget);

        if (HpFraction(boss) < 0.40)
            return OnSelf(boss, "canon.skill.reformation")
                ?? Strike(boss, "canon.skill.contact", LowestHpPlayer(combat, boss));

        return Chance(combat, boss, "eclat", 0.60)
            ? Strike(boss, "canon.skill.eclat-vitrifie", LowestHpPlayer(combat, boss))
            : Strike(boss, "canon.skill.contact", LowestHpPlayer(combat, boss));
    }
}
