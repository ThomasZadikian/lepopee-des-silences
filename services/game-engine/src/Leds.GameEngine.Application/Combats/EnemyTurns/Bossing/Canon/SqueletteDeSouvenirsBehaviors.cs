using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Bestiaire, famille "Les Squelettes de Souvenirs" — scripts déterministes pour
/// les trois créatures. La mécanique de famille "L'Ossuaire" (relance via un
/// Ossement laissé au sol) n'est pas modélisée — voir le commentaire dans
/// CatalogSeedRunner.SeedBestiaireSqueletteDeSouvenirsAsync pour le détail des
/// approximations correspondantes (Effondrement, Braise mémorielle).
/// <para>
/// "Face à un coup très puissant" (≥25% MaxVitality en un coup) EST câblée pour
/// les 3 créatures (Chantier Bestiaire Phase 13). Le Porteur de Cendre a un sort
/// défensif dédié (Braise mémorielle) qui l'utilise directement ; le Squelette de
/// Souvenir et le Chœur Muet, sans alternative défensive dans leur kit de 4,
/// ripostent contre leur dernier agresseur (<see cref="CanonBossBehaviorBase.LastAttacker"/>)
/// avec leur sort le plus offensif.
/// </para>
/// </summary>
public sealed class SqueletteSouvenirBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.squelette-souvenir";

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
            var retaliation = Strike(boss, "canon.skill.fragment-grave", LastAttacker(combat, boss))
                ?? Strike(boss, "canon.skill.griffe-dos", LastAttacker(combat, boss));
            if (retaliation is not null) return retaliation;
        }

        var porteurAlive = combat.Enemies.Any(e =>
            !e.IsDefeated && string.Equals(e.SourceKey, "canon.enemy.porteur-cendre", StringComparison.OrdinalIgnoreCase));

        if (HpFraction(boss) < 0.20 && porteurAlive)
            return OnAllPlayers(boss, "canon.skill.effondrement", combat);

        // Pas de suivi "dernier lanceur de sort" côté moteur — la doc en fait la
        // cible par défaut ; approximé par la cible la plus faible en PV, comme
        // ailleurs dans le bestiaire.
        var target = LowestHpPlayer(combat, boss);

        // Cycle pondéré 50/30/20 via deux tirages imbriqués indépendants :
        // P(Griffe)=0.50 ; P(¬Griffe)·P(Fragment)=0.50·0.60=0.30 ; reste 0.20 Étreinte.
        if (Chance(combat, boss, "griffe-dos", 0.50))
            return Strike(boss, "canon.skill.griffe-dos", target);

        return Chance(combat, boss, "fragment-grave", 0.60)
            ? Strike(boss, "canon.skill.fragment-grave", target)
            : Strike(boss, "canon.skill.etreinte-creuse", target);
    }
}

/// <summary>
/// Le Porteur de Cendre — soutien du groupe. Sa priorité absolue documentée
/// ("Braise mémorielle si un Ossement est disponible") est absente ici : aucun
/// suivi d'Ossement n'existe côté moteur (voir la note de classe dans
/// SqueletteSouvenirBossBehavior) — il passe directement au soin d'urgence, puis
/// harcèle. Braise mémorielle sert désormais de réaction "Attitude en combat"
/// (garde défensive face à un coup très puissant), sa seule utilisation par l'IA.
/// </summary>
public sealed class PorteurCendreBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.porteur-cendre";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Attitude en combat — face à un coup très puissant : rallume un
        // Ossement — pour l'instant, se replie prudemment (voir commentaire de
        // classe). Prend priorité sur le plan normal.
        if (TookPowerfulHit(boss))
            return OnSelf(boss, "canon.skill.braise-memorielle");

        var mostWoundedAlly = MostWoundedAlly(combat, boss);
        if (mostWoundedAlly is not null && HpFraction(mostWoundedAlly) < 0.40)
            return Strike(boss, "canon.skill.fardeau-partage", mostWoundedAlly);

        if (Chance(combat, boss, "jet-de-cendre", 0.60))
            return Strike(boss, "canon.skill.jet-de-cendre", LowestHpPlayer(combat, boss));

        var undotted = PlayerWithoutDot(combat, boss);
        return Strike(boss, "canon.skill.sursaut-memoriel", undotted ?? LowestHpPlayer(combat, boss))
            ?? Strike(boss, "canon.skill.jet-de-cendre", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// Le Chœur Muet — élite de contrôle. Ouvre par Berceuse inversée (80% des cas),
/// réduit au silence, puis achève toute cible déjà réduite au silence avec Note
/// tenue.
/// </summary>
public sealed class ChoeurMuetBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.choeur-muet";

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
            var retaliation = Strike(boss, "canon.skill.note-tenue", LastAttacker(combat, boss))
                ?? Strike(boss, "canon.skill.lecture-des-silences", LastAttacker(combat, boss));
            if (retaliation is not null) return retaliation;
        }

        if (combat.TurnNumber == 1 && Chance(combat, boss, "berceuse-ouverture", 0.80))
            return OnAllPlayers(boss, "canon.skill.berceuse-inversee", combat);

        var silenced = PlayerWithStatus(combat, "canon.skill.silence:Silence");
        if (silenced is not null)
            return Strike(boss, "canon.skill.note-tenue", silenced);

        if (combat.TurnNumber == 1)
            return Strike(boss, "canon.skill.silence", FastestPlayer(combat, boss));

        return Strike(boss, "canon.skill.lecture-des-silences", LowestHpPlayer(combat, boss))
            ?? Strike(boss, "canon.skill.silence", FastestPlayer(combat, boss));
    }
}
