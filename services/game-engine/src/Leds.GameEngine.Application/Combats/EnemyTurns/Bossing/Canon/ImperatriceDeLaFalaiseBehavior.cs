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
/// </summary>
public sealed class ImperatriceBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.imperatrice";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;
        var hpFraction = HpFraction(boss);

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
