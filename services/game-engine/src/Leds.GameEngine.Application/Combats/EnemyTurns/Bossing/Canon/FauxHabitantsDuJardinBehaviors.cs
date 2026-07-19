using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Faux Habitants du Jardin" — scripts déterministes pour
/// les deux créatures. La mécanique de famille "La Boucle" (rejoue le tour 1 tous
/// les 3 tours) n'est pas modélisée — voir le commentaire dans
/// CatalogSeedRunner.SeedBestiaireFauxHabitantsDuJardinAsync.
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée pour
/// les 2 créatures (Chantier Bestiaire Phase 13) : le Promeneur Figé sur soi (Pas
/// de promenade, garde) ; le Jardinier Sans Ombre avec Paillage, un sort ciblant
/// normalement un allié blessé, appliqué à lui-même (« le massif est protégé pour
/// l'hiver »).
/// </para>
/// </summary>
public sealed class PromeneurFigeBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.promeneur-fige";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : toutes les
        // quarante secondes, exactement (Pas de promenade, sur soi). Prend
        // priorité sur le plan normal.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.pas-de-promenade");

        // Cycle pondéré 50/30/20 (deux tirages imbriqués indépendants, même
        // convention que le Squelette de Souvenir).
        if (Chance(combat, boss, "salut", 0.50))
            return Strike(boss, "canon.skill.salut-de-chapeau", LowestHpPlayer(combat, boss));

        if (Chance(combat, boss, "sifflotement", 0.60))
        {
            var targets = LivingPlayers(combat).Take(2).ToArray();
            return targets.Length > 0 && Owns(boss, "canon.skill.sifflotement")
                ? new BossActionDecision("canon.skill.sifflotement", targets.Select(p => p.Id.Value).ToArray())
                : null;
        }

        return Strike(boss, "canon.skill.conversation-tranquille", LowestHpPlayer(combat, boss))
            ?? OnSelf(boss, "canon.skill.pas-de-promenade");
    }
}

/// <summary>
/// Le Jardinier Sans Ombre — anti-préparation. Purge en priorité les cibles
/// multi-buffées, secourt un allié blessé, sinon taille la plus fragile.
/// </summary>
public sealed class JardinierSansOmbreBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.jardinier-sans-ombre";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : le massif est
        // protégé pour l'hiver (Paillage, sur soi — un sort normalement destiné
        // à un allié blessé). Prend priorité sur le plan normal.
        if (TookPowerfulHit(boss))
            return Strike(boss, "canon.skill.paillage", boss);

        var multiBuffed = LivingPlayers(combat)
            .FirstOrDefault(p => p.StatusEffects.Count(e => e.Kind == StatusEffectKind.StatModifier && e.Magnitude > 0) >= 2);
        if (multiBuffed is not null)
            return Strike(boss, "canon.skill.emondage", multiBuffed);

        var woundedAlly = MostWoundedAlly(combat, boss);
        if (woundedAlly is not null && HpFraction(woundedAlly) < 0.50)
            return Strike(boss, "canon.skill.greffe", woundedAlly)
                ?? Strike(boss, "canon.skill.paillage", woundedAlly);

        return Strike(boss, "canon.skill.secateur", LowestHpPlayer(combat, boss));
    }
}
