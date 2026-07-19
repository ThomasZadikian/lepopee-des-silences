using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Échos d'Émotions" — scripts déterministes pour les
/// trois créatures. La mécanique de famille "La Dissonance" n'est pas modélisée
/// — voir le commentaire dans CatalogSeedRunner.SeedBestiaireEchosDEmotionsAsync.
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée pour
/// les 3 créatures (Chantier Bestiaire Phase 13). L'Écho de Peur a un sort
/// défensif dédié (Saccade) dans son kit de 4 ; l'Écho de Colère, sans
/// alternative défensive, explose de rage (Explosion, si le mana le permet) ou
/// riposte contre son dernier agresseur sinon ; l'Écho de Tristesse réutilise son
/// soin partagé (Silence partagé, normalement réservé à ≤40% PV) hors seuil.
/// </para>
/// </summary>
public sealed class EchoColereBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.echo-colere";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : explose de rage
        // si le mana le permet, sinon riposte contre son dernier agresseur.
        // Prend priorité sur le plan normal ; si ni l'un ni l'autre n'aboutit,
        // continue sur le plan normal.
        if (TookPowerfulHit(boss))
        {
            var reaction = boss.Mana >= 16
                ? OnAllPlayers(boss, "canon.skill.explosion", combat)
                : null;
            reaction ??= Strike(boss, "canon.skill.constat-sec", LastAttacker(combat, boss))
                ?? Strike(boss, "canon.skill.eclat-echo-colere", LastAttacker(combat, boss));
            if (reaction is not null) return reaction;
        }

        if (HpFraction(boss) < 0.50 && boss.Mana >= 16)
            return OnAllPlayers(boss, "canon.skill.explosion", combat);

        if (boss.LastAttackerId is { } attackerId)
        {
            var attacker = LivingPlayers(combat).FirstOrDefault(p => p.Id.Value == attackerId);
            if (attacker is not null)
                return Strike(boss, "canon.skill.constat-sec", attacker)
                    ?? Strike(boss, "canon.skill.eclat-echo-colere", attacker);
        }

        if (combat.TurnNumber == 1)
            return OnSelf(boss, "canon.skill.montee")
                ?? Strike(boss, "canon.skill.eclat-echo-colere", LowestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.eclat-echo-colere", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// L'Écho de Peur — harceleur évasif. Alterne systématiquement une action
/// offensive et une Saccade défensive, névrose les cibles vierges de DoT.
/// </summary>
public sealed class EchoPeurBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.echo-peur";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : là où vous
        // regardez, il n'est déjà plus (Saccade). Prend priorité sur le plan
        // normal, y compris l'alternance de tours ci-dessous.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.saccade")
                ?? Strike(boss, "canon.skill.frisson", LowestHpPlayer(combat, boss));

        if (combat.TurnNumber % 2 == 0)
            return OnSelf(boss, "canon.skill.saccade")
                ?? Strike(boss, "canon.skill.frisson", LowestHpPlayer(combat, boss));

        var undotted = PlayerWithoutDot(combat, boss);
        if (undotted is not null)
            return Strike(boss, "canon.skill.nevrose", undotted);

        return Strike(boss, "canon.skill.porte-fermee", FastestPlayer(combat, boss))
            ?? Strike(boss, "canon.skill.frisson", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// L'Écho de Tristesse — tank d'usure inversé. Alourdit le plus rapide à
/// l'ouverture, entretient le DoT le plus lent, et partage un répit sincère
/// (soin de tout le monde, alliés et adversaires) sous 40% PV.
/// </summary>
public sealed class EchoTristesseBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.echo-tristesse";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        var sharedRespite = combat.Enemies.Concat(combat.Allies).Where(c => !c.IsDefeated).ToArray();

        // Attitude en combat — face à un coup très puissant : partage un répit
        // sincère avec tout le monde, alliés et adversaires (normalement
        // réservé à ≤40% PV). Prend priorité sur le plan normal.
        if (TookPowerfulHit(boss) && sharedRespite.Length > 0 && Owns(boss, "canon.skill.silence-partage"))
            return new BossActionDecision("canon.skill.silence-partage", sharedRespite.Select(c => c.Id.Value).ToArray());

        if (HpFraction(boss) < 0.40)
        {
            return sharedRespite.Length > 0 && Owns(boss, "canon.skill.silence-partage")
                ? new BossActionDecision("canon.skill.silence-partage", sharedRespite.Select(c => c.Id.Value).ToArray())
                : null;
        }

        if (combat.TurnNumber == 1)
            return Strike(boss, "canon.skill.poids", FastestPlayer(combat, boss));

        return Strike(boss, "canon.skill.sursaut-memoriel", LowestMagicDefensePlayer(combat, boss))
            ?? Strike(boss, "canon.skill.constat-tardif", LowestHpPlayer(combat, boss));
    }
}
