using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Gardiens de Crystal" — scripts déterministes pour les
/// deux créatures. La mécanique de famille "La Résonance" n'est pas modélisée —
/// voir le commentaire dans CatalogSeedRunner.SeedBestiaireGardiensDeCrystalAsync.
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée pour
/// les 2 créatures (Chantier Bestiaire Phase 13), chacune via un sort défensif
/// déjà présent dans son kit de 4 : Gardien Intemporel → Rempart (garde de
/// groupe), Éclat Éveillé → Prisme (garde).
/// </para>
/// </summary>
public sealed class GardienIntemporelBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.gardien-intemporel";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : élève un rempart
        // de groupe. Prend priorité sur le plan normal, y compris le premier
        // tour ci-dessous.
        if (TookPowerfulHit(boss))
            return OnAllAllies(boss, "canon.skill.rempart", combat)
                ?? OnSelf(boss, "canon.skill.rempart");

        var eclatPresent = combat.Enemies.Any(e =>
            !e.IsDefeated && string.Equals(e.SourceKey, "canon.enemy.eclat-eveille", StringComparison.OrdinalIgnoreCase));
        if (combat.TurnNumber == 1 && eclatPresent)
            return OnAllAllies(boss, "canon.skill.rempart", combat)
                ?? OnSelf(boss, "canon.skill.rempart");

        // Pas de suivi "nombre de sorts magiques lancés" côté moteur — approximé par
        // le M.ATQ effectif le plus élevé (le plus probable lanceur de sorts).
        var mostArcane = LivingPlayers(combat)
            .OrderByDescending(p => p.EffectiveMagicAttack)
            .ThenBy(p => DeterministicTieKey(combat, boss, p))
            .FirstOrDefault();
        if (mostArcane is not null && mostArcane.EffectiveMagicAttack > 0 && Chance(combat, boss, "stase", 0.50))
            return Strike(boss, "canon.skill.stase", mostArcane);

        return Chance(combat, boss, "refraction", 0.60)
            ? Strike(boss, "canon.skill.refraction", LowestHpPlayer(combat, boss))
            : Strike(boss, "canon.skill.poing-de-crystal", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// L'Éclat Éveillé — artillerie fragile. Se réoriente, pulse, puis déchaîne la
/// Flamme Séraphine tant qu'il conserve son mana de réserve.
/// </summary>
public sealed class EclatEveilleBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.eclat-eveille";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : la lumière ne
        // s'arrête pas, elle se partage (Prisme, garde). Prend priorité sur le
        // plan normal, y compris les deux premiers tours ci-dessous.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.prisme")
                ?? Strike(boss, "canon.skill.pulsation", LowestHpPlayer(combat, boss));

        if (combat.TurnNumber == 1)
            return OnSelf(boss, "canon.skill.facette")
                ?? Strike(boss, "canon.skill.pulsation", LowestHpPlayer(combat, boss));

        if (combat.TurnNumber == 2)
            return Strike(boss, "canon.skill.pulsation", LowestHpPlayer(combat, boss))
                ?? OnSelf(boss, "canon.skill.facette");

        // Ne dépense jamais sous 12 mana : garde de quoi lancer Flamme Séraphine.
        if (boss.Mana >= 12)
            return Strike(boss, "canon.skill.flamme-seraphine", LowestHpPlayer(combat, boss));

        return OnSelf(boss, "canon.skill.prisme")
            ?? Strike(boss, "canon.skill.pulsation", LowestHpPlayer(combat, boss));
    }
}
