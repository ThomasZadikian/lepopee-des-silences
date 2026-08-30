using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Copistes" — scripts déterministes pour les quatre
/// créatures.
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée pour
/// les 4 créatures (Chantier Bestiaire Phase 13). Aucune n'a de sort défensif/
/// auto-ciblé dans son kit de 4 (invariant SFD §I) sauf l'Encrier Vivant (Corps de
/// verre) et la Page Inachevée (Repli de papier), qui l'utilisent directement ; le
/// Copiste Aveugle et le Relieur, sans alternative défensive, ripostent contre
/// leur dernier agresseur (<see cref="CanonBossBehaviorBase.LastAttacker"/>) avec
/// leur sort le plus offensif — approximation documentée, faute d'effet "riposte"
/// dédié authorable aujourd'hui.
/// </para>
/// <para>
/// Le mécanisme de famille "La Marge" (les effets sur la durée posés par un
/// Copiste sont "écrits" ; tuer un Copiste en train de canaliser inflige Rature
/// aux alliés) n'est pas modélisé — nécessiterait un hook "on ally death" qui
/// n'existe pas encore côté moteur (même famille de limitation que "L'Ossuaire"
/// pour les Squelettes de Souvenirs). Différé, pas silencieusement ignoré.
/// </para>
/// </summary>
public sealed class CopisteAveugleBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.copiste-aveugle";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : aucun sort défensif
        // dans son kit, riposte contre son dernier agresseur avec son sort le plus
        // offensif. Prend priorité sur le plan normal ; si aucun agresseur vivant
        // n'est identifiable, continue sur le plan normal ci-dessous.
        if (TookPowerfulHit(boss))
        {
            var retaliation = Strike(boss, "canon.skill.lecture-des-silences", LastAttacker(combat, boss))
                ?? Strike(boss, "canon.skill.plume-seche", LastAttacker(combat, boss));
            if (retaliation is not null) return retaliation;
        }

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

        // Attitude en combat — face à un coup très puissant : durcit sa paroi
        // fêlée. Prend priorité sur le plan normal.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.corps-de-verre");

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

        // Attitude en combat — face à un coup très puissant : se plie sur
        // elle-même. Prend priorité sur le plan normal.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.repli-de-papier");

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

        // Attitude en combat — face à un coup très puissant : aucun sort
        // défensif dans son kit, riposte contre son dernier agresseur avec son
        // sort le plus offensif. Prend priorité sur le plan normal ; si aucun
        // agresseur vivant n'est identifiable, continue sur le plan normal.
        if (TookPowerfulHit(boss))
        {
            var retaliation = Strike(boss, "canon.skill.noeud-final", LastAttacker(combat, boss))
                ?? Strike(boss, "canon.skill.couture", LastAttacker(combat, boss));
            if (retaliation is not null) return retaliation;
        }

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
