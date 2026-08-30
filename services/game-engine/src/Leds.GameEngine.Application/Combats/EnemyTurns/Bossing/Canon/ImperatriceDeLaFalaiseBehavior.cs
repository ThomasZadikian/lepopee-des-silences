using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire — L'Impératrice de la Falaise, mini-boss unique par run. N'appartient
/// à aucune famille. Trois phases sur les PV : ouverture par Déluge du Styx,
/// Symphonie des enfers dès 60% PV, exécution prioritaire par Lame de fond dès
/// 25% PV sur toute cible déjà lourdement affectée par des DoT. Ne cible jamais
/// un adversaire à 100% PV avec Lame de fond ("la mer ne frappe que ce qui est
/// déjà entamé").
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée
/// (Chantier Bestiaire Phase 13). Aucun de ses 5 sorts n'est défensif/auto-ciblé
/// — elle riposte contre son dernier agresseur
/// (<see cref="CanonBossBehaviorBase.LastAttacker"/>) avec Lame de fond renforcée
/// (Lame de fond simple si elle ne la possède pas), mais seulement s'il est déjà
/// entamé ("la mer ne frappe que ce qui est déjà entamé" — même contrainte que le
/// reste du kit) ; sinon, continue sur le plan normal ci-dessous.
/// </para>
/// </summary>
public sealed class ImperatriceBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.imperatrice";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;
        var hpFraction = HpFraction(boss);

        // Attitude en combat — face à un coup très puissant : riposte contre
        // son dernier agresseur avec Lame de fond, s'il est déjà entamé. Prend
        // priorité sur le plan normal ; si aucun agresseur vivant n'est
        // identifiable ou qu'il est encore à 100% PV, continue sur le plan
        // normal (Lame de fond ne frappe jamais une cible intacte).
        if (TookPowerfulHit(boss))
        {
            var attacker = LastAttacker(combat, boss);
            if (attacker is not null && attacker.CurrentVitality < attacker.MaxVitality)
            {
                var retaliation = Strike(boss, "canon.skill.lame-de-fond-renforcee", attacker)
                    ?? Strike(boss, "canon.skill.lame-de-fond", attacker);
                if (retaliation is not null) return retaliation;
            }
        }

        if (combat.TurnNumber == 1)
            return OnAllPlayers(boss, "canon.skill.deluge-du-styx", combat);

        var damagedTarget = LowestHpPlayer(combat, boss);
        var lameCanFire = damagedTarget is not null && damagedTarget.CurrentVitality < damagedTarget.MaxVitality;

        if (hpFraction <= 0.25 && lameCanFire)
        {
            var heavilyDotted = damagedTarget!.StatusEffects.Count(e => e.Kind == StatusEffectKind.DamageOverTime) >= 2;
            return heavilyDotted
                ? Strike(boss, "canon.skill.lame-de-fond-renforcee", damagedTarget)
                    ?? Strike(boss, "canon.skill.lame-de-fond", damagedTarget)
                : Strike(boss, "canon.skill.lame-de-fond", damagedTarget);
        }

        if (hpFraction <= 0.60 && PlayerWithoutDot(combat, boss) is not null)
            return OnAllPlayers(boss, "canon.skill.symphonie-des-enfers", combat);

        if (lameCanFire && Chance(combat, boss, "lame-de-fond", 0.50))
            return Strike(boss, "canon.skill.lame-de-fond", damagedTarget);

        return OnAllPlayers(boss, "canon.skill.maree-montante", combat)
            ?? (lameCanFire ? Strike(boss, "canon.skill.lame-de-fond", damagedTarget) : null);
    }
}
