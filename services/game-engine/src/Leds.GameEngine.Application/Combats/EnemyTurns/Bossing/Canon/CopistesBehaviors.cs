using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Copistes" — scripts déterministes pour les quatre
/// créatures. Le mécanisme de famille "La Marge" (les effets sur la durée posés
/// par un Copiste sont "écrits" ; tuer un Copiste en train de canaliser inflige
/// Rature aux alliés) n'est pas modélisé — nécessiterait un hook "on ally death"
/// qui n'existe pas encore côté moteur (même famille de limitation que
/// "Attitude en combat" pour les Veilleurs du Seuil). Différé, pas silencieusement
/// ignoré.
/// </summary>
public sealed class CopisteAveugleBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.copiste-aveugle";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        if (combat.TurnNumber == 1)
            return Strike(boss, "canon.skill.dictee", LowestMagicDefensePlayer(combat, boss));

        if (combat.TurnNumber == 2)
        {
            var marked = PlayerWithStatus(combat, "canon.skill.dictee:StatModifier");
            if (marked is not null)
                return Strike(boss, "canon.skill.sursaut-memoriel", marked);
        }

        return boss.Mana < 10
            ? Strike(boss, "canon.skill.plume-seche", LowestHpPlayer(combat, boss))
            : Strike(boss, "canon.skill.lecture-des-silences", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// L'Encrier Vivant — réservoir tactique : recharge en priorité un allié Copiste à
/// sec, sinon marque une cible encore vierge de DoT, sinon éclabousse un groupe
/// ou se protège sous pression.
/// </summary>
public sealed class EncrierVivantBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.encrier-vivant";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        var starvedAlly = MostManaStarvedAlly(combat, boss, threshold: 12);
        if (starvedAlly is not null)
            return Strike(boss, "canon.skill.recharge", starvedAlly);

        var unmarked = PlayerWithoutDot(combat, boss);
        if (unmarked is not null)
            return Strike(boss, "canon.skill.encre-vive", unmarked);

        if (LivingPlayers(combat).Count >= 3)
            return Chance(combat, boss, "eclaboussure", 0.55)
                ? OnAllPlayers(boss, "canon.skill.eclaboussure", combat)
                : Strike(boss, "canon.skill.encre-vive", LowestHpPlayer(combat, boss));

        if (HpFraction(boss) < 0.50)
            return OnSelf(boss, "canon.skill.corps-de-verre")
                ?? Strike(boss, "canon.skill.encre-vive", LowestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.encre-vive", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// La Page Inachevée — contrôle pur : réduit au silence en priorité, punit toute
/// cible déjà réduite au silence, sinon efface un buff.
/// </summary>
public sealed class PageInacheveeBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.page-inachevee";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        var silenced = LivingPlayers(combat).FirstOrDefault(p => p.IsSilenced);
        if (silenced is not null)
            return Strike(boss, "canon.skill.phrase-inachevee", silenced);

        // Pas de suivi "dernier lanceur de soin" côté moteur — approximé par la
        // cible la plus rapide (celle qui agira en premier, donc la plus utile à
        // faire taire) plutôt que par la véritable détection décrite dans le PDF.
        if (combat.TurnNumber == 1)
            return Strike(boss, "canon.skill.silence", FastestPlayer(combat, boss));

        return Chance(combat, boss, "marge-blanche", 0.65)
            ? Strike(boss, "canon.skill.marge-blanche", LowestHpPlayer(combat, boss))
            : Strike(boss, "canon.skill.phrase-inachevee", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// Le Relieur — pièce maîtresse des combos DoT : allonge la souffrance déjà en
/// place, exécute sous 35% PV, sinon lie les adversaires ou martèle le plus lent.
/// </summary>
public sealed class RelieurBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.relieur";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        var heavilyDotted = LivingPlayers(combat)
            .FirstOrDefault(p => p.StatusEffects.Count(e => e.Kind == StatusEffectKind.DamageOverTime) >= 2);

        if (heavilyDotted is not null)
        {
            if (HpFraction(heavilyDotted) < 0.35 && Chance(combat, boss, "noeud-final", 0.30))
                return Strike(boss, "canon.skill.noeud-final", heavilyDotted);

            return Strike(boss, "canon.skill.ecriture-continuelle", heavilyDotted)
                ?? Strike(boss, "canon.skill.couture", SlowestPlayer(combat, boss));
        }

        if (combat.TurnNumber == 1 && LivingPlayers(combat).Count >= 2)
            return Strike(boss, "canon.skill.reliure-de-chair", LowestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.couture", SlowestPlayer(combat, boss));
    }
}
