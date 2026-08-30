using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Blouses Blanches" — scripts déterministes pour les
/// trois créatures. La mécanique de famille "Le Dossier" (consignation
/// cumulative des types d'action adverses, -10% de stats une fois les 4 types
/// observés) n'est pas modélisée — voir le commentaire dans
/// CatalogSeedRunner.SeedBestiaireBlousesBlanchesAsync.
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée pour
/// les 3 créatures (Chantier Bestiaire Phase 13). L'Infirmière du Déni (Placebo)
/// et le Souvenir Alité (Sonnette) ont un sort défensif dédié dans leur kit de 4
/// et l'utilisent directement ; le Régisseur des Couloirs Blancs, sans
/// alternative défensive, riposte contre son dernier agresseur
/// (<see cref="CanonBossBehaviorBase.LastAttacker"/>) avec son sort le plus
/// offensif à la place.
/// </para>
/// </summary>
public sealed class InfirmiereDeniBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.infirmiere-deni";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : le prochain soin
        // reçu par la cible est nié (Placebo, sur soi). Prend priorité sur le
        // plan normal, y compris le premier tour.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.placebo");

        if (combat.TurnNumber == 1)
            return OnSelf(boss, "canon.skill.placebo");

        var buffedTarget = LivingPlayers(combat).FirstOrDefault(HasAnyPositiveStatModifier);
        if (buffedTarget is not null && Chance(combat, boss, "injection-blanche", 0.70))
            return Strike(boss, "canon.skill.injection-blanche", buffedTarget);

        return Strike(boss, "canon.skill.bordage", FastestPlayer(combat, boss))
            ?? Strike(boss, "canon.skill.anagramme", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// Le Souvenir Alité — ennemi d'usure. Empoisonne les cibles saines, sinon
/// harcèle les plus fragiles.
/// </summary>
public sealed class SouvenirAliteBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.souvenir-alite";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : appelle
        // (Sonnette, sur soi). Prend priorité sur le plan normal.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.sonnette");

        var undotted = PlayerWithoutDot(combat, boss);
        if (undotted is not null)
            return Strike(boss, "canon.skill.nevrose", undotted);

        var infirmiereAlive = combat.Enemies.Any(e =>
            !e.IsDefeated && string.Equals(e.SourceKey, "canon.enemy.infirmiere-deni", StringComparison.OrdinalIgnoreCase));
        if (infirmiereAlive && Chance(combat, boss, "sonnette", 0.50))
            return OnSelf(boss, "canon.skill.sonnette");

        return Strike(boss, "canon.skill.visite", LeastDefendedPlayer(combat, boss))
            ?? Strike(boss, "canon.skill.drap-tendu", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// Le Régisseur des Couloirs Blancs — contrôleur rigide. Enchaîne toujours la
/// même séquence (Contemplation → Tour de clef → Extinction des feux) sur la
/// même cible, sans jamais s'adapter — c'est sa faille documentée.
/// </summary>
public sealed class RegisseurBlancBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.regisseur-blanc";

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
            var retaliation = Strike(boss, "canon.skill.extinction-des-feux", LastAttacker(combat, boss))
                ?? Strike(boss, "canon.skill.trousseau", LastAttacker(combat, boss));
            if (retaliation is not null) return retaliation;
        }

        var slowed = PlayerWithStatus(combat, "canon.skill.contemplation-infinie:StatModifier");
        var locked = PlayerWithStatus(combat, "canon.skill.tour-de-clef:StatModifier");

        if (slowed is not null && locked is not null && slowed.Id == locked.Id)
            return Strike(boss, "canon.skill.extinction-des-feux", slowed);

        if (slowed is not null && locked is null)
            return Strike(boss, "canon.skill.tour-de-clef", slowed);

        if (slowed is null)
            return Strike(boss, "canon.skill.contemplation-infinie", HighestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.trousseau", LowestHpPlayer(combat, boss));
    }
}
