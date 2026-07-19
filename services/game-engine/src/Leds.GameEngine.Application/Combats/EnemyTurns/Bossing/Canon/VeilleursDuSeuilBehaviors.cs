using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Veilleurs du Seuil" — scripts déterministes pour les
/// quatre créatures.
/// <para>
/// Le suivi précis "qui a attaqué quel allié ce tour" décrit dans le PDF
/// (déclencheur de Seuil souillé) reste approximé par un ciblage prioritaire
/// déterministe (plus faible/plus rapide en PV), pas par le dernier agresseur
/// réel — un vrai suivi "par allié" n'existe pas encore côté moteur.
/// </para>
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée
/// pour la Sentinelle du Seuil via <see cref="CanonBossBehaviorBase.TookPowerfulHit"/>
/// (Chantier Bestiaire Phase 11/12). Les 3 autres réactions "Attitude en combat"
/// de cette famille à ce même déclencheur (Veilleur du Tapis, Porteur de Plateau,
/// Écho de Politesse) demandent chacune un effet nouveau non encore authorable
/// (garde gratuite hors action, soin boosté ponctuel, esquive garantie une fois
/// par combat) — non câblées, prochaine passe. "Le Protocole" (mécanique de
/// famille) et les 4 autres lignes d'Attitude en combat par créature restent
/// également non câblées.
/// </para>
/// </summary>
public sealed class VeilleurTapisBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.veilleur-tapis";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        var livingGuards = combat.Enemies.Count(e => !e.IsDefeated);

        // Tour 1 : prend position en rempart si au moins un autre Veilleur est présent.
        if (combat.TurnNumber == 1 && livingGuards >= 2)
            return OnAllAllies(boss, "canon.skill.rempart", combat)
                ?? OnSelf(boss, "canon.skill.rempart");

        // Seul survivant : cesse de garder, harcèle la cible la plus rapide.
        if (livingGuards <= 1)
        {
            var fastest = FastestPlayer(combat, boss);
            return combat.TurnNumber % 2 == 0
                ? Strike(boss, "canon.skill.etouffement-feutre", fastest)
                : Strike(boss, "canon.skill.pli-du-tapis", fastest);
        }

        // Punition du protocole : châtie la cible la plus faible (proxy déterministe
        // de "quiconque a frappé un Veilleur ce tour", voir note de classe ci-dessus).
        return Strike(boss, "canon.skill.seuil-souille", LowestHpPlayer(combat, boss))
            ?? Strike(boss, "canon.skill.pli-du-tapis", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// Le Porteur de Plateau — soigne l'allié le plus abîmé, resserre le service au
/// premier tour, punit sinon d'une tasse retournée.
/// </summary>
public sealed class PorteurPlateauBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.porteur-plateau";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        var mostWoundedAlly = MostWoundedAlly(combat, boss);
        if (mostWoundedAlly is not null && HpFraction(mostWoundedAlly) <= 0.60)
            return Strike(boss, "canon.skill.service-du-the", mostWoundedAlly);

        if (combat.TurnNumber == 1)
            return OnAllAllies(boss, "canon.skill.etiquette", combat)
                ?? Strike(boss, "canon.skill.tasse-retournee", HighestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.tasse-retournee", HighestHpPlayer(combat, boss))
            ?? Strike(boss, "canon.skill.service-du-the", mostWoundedAlly);
    }
}

/// <summary>
/// L'Écho de Politesse — brouille la portée au premier tour, se protège sous
/// pression, harcèle sinon de mots vides.
/// </summary>
public sealed class EchoPolitesseBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.echo-politesse";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        if (combat.TurnNumber == 1)
            return OnAllPlayers(boss, "canon.skill.brume", combat)
                ?? Strike(boss, "canon.skill.formule-creuse", LowestHpPlayer(combat, boss));

        if (HpFraction(boss) <= 0.50)
            return OnSelf(boss, "canon.skill.courbette-inversee")
                ?? Strike(boss, "canon.skill.formule-creuse", LowestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.formule-creuse", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// La Sentinelle du Seuil — élite. Exécute la cible dont le protocole est
/// complet (-DEF et -Focus actifs simultanément), sinon marche vers le plus
/// faible, se replie en socle sous pression.
/// </summary>
public sealed class SentinelleSeuilBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.sentinelle-seuil";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : "Socle immédiat au tour
        // suivant. Un pilier ne tombe pas deux fois." Takes priority over the normal
        // plan below, matching the SFD's framing of Attitude en combat as a reaction
        // that overrides standing priorities, not just another item in the same list.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.socle");

        var judged = LivingPlayers(combat)
            .FirstOrDefault(p => HasNegativeStatModifier(p, CombatStat.Defense)
                               && HasNegativeStatModifier(p, CombatStat.Focus));
        if (judged is not null)
            return Strike(boss, "canon.skill.verdict-du-seuil", judged);

        if (combat.TurnNumber == 1)
            return OnSelf(boss, "canon.skill.socle")
                ?? Strike(boss, "canon.skill.chute-de-marbre", LowestHpPlayer(combat, boss));

        if (HpFraction(boss) <= 0.35)
            return OnSelf(boss, "canon.skill.socle")
                ?? Strike(boss, "canon.skill.chute-de-marbre", LowestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.chute-de-marbre", LowestHpPlayer(combat, boss));
    }
}
