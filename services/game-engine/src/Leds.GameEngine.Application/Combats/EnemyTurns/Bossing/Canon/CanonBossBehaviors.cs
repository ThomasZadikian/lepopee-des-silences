using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;

/// <summary>
/// Shared helpers for the canon room-boss scripts. All decisions are deterministic
/// (HP thresholds + turn number only — no randomness), so combats replay identically.
/// A behavior returns <c>null</c> to defer the turn to the generic utility AI, and
/// only ever names a skill the boss actually owns (otherwise the resolver falls back),
/// so a boss missing its kit degrades gracefully instead of stalling.
/// </summary>
public abstract class CanonBossBehaviorBase : IBossBehavior
{
    public abstract string BossKey { get; }

    public abstract BossActionDecision? DecideAction(BossDecisionContext context);

    protected static double HpFraction(Combatant c)
        => c.MaxVitality > 0 ? (double)c.CurrentVitality / c.MaxVitality : 0.0;

    protected static IReadOnlyList<Combatant> LivingPlayers(Combat combat)
        => combat.Allies.Where(a => !a.IsDefeated).ToArray();

    /// <summary>The weakest living player (focus-fire / finisher target). Null if none.
    /// Ties (equal current vitality — e.g. nobody's been hit yet) are broken by a
    /// deterministic per-candidate hash, never by array position, so this doesn't
    /// silently default to the protagonist every time.</summary>
    protected static Combatant? LowestHpPlayer(Combat combat, Combatant boss)
        => LivingPlayers(combat)
            .OrderBy(a => a.CurrentVitality)
            .ThenBy(a => DeterministicTieKey(combat, boss, a))
            .FirstOrDefault();

    /// <summary>The healthiest living player (best target for control / drain). Null if none.
    /// Same deterministic tie-break as <see cref="LowestHpPlayer"/>.</summary>
    protected static Combatant? HighestHpPlayer(Combat combat, Combatant boss)
        => LivingPlayers(combat)
            .OrderByDescending(a => a.CurrentVitality)
            .ThenBy(a => DeterministicTieKey(combat, boss, a))
            .FirstOrDefault();

    /// <summary>
    /// A reproducible-but-not-array-order hash used to break exact ties in HP-based
    /// target selection (turn 1, nobody hurt yet, etc.).
    /// </summary>
    private static double DeterministicTieKey(Combat combat, Combatant actor, Combatant candidate)
        => DeterministicCombatRoll.UnitInterval(
            $"boss-target:{combat.Id.Value}:{combat.TurnNumber}:{actor.Id.Value}:{candidate.Id.Value}");

    protected static bool Owns(Combatant boss, string skillKey)
        => boss.Skills.Any(s => string.Equals(s.Key, skillKey, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Builds a decision for an offensive single-target skill against <paramref name="target"/>,
    /// but only if the boss owns the skill and the target exists; otherwise null (defer).
    /// </summary>
    protected static BossActionDecision? Strike(Combatant boss, string skillKey, Combatant? target)
        => target is not null && Owns(boss, skillKey)
            ? new BossActionDecision(skillKey, [target.Id.Value])
            : null;

    /// <summary>Builds a self-targeted decision (buff/guard) if the boss owns the skill.</summary>
    protected static BossActionDecision? OnSelf(Combatant boss, string skillKey)
        => Owns(boss, skillKey)
            ? new BossActionDecision(skillKey, [boss.Id.Value])
            : null;

    /// <summary>Builds a decision hitting every living player (AoE skill on the boss's opposing side).</summary>
    protected static BossActionDecision? OnAllPlayers(Combatant boss, string skillKey, Combat combat)
    {
        var targets = LivingPlayers(combat);
        return targets.Count > 0 && Owns(boss, skillKey)
            ? new BossActionDecision(skillKey, targets.Select(p => p.Id.Value).ToArray())
            : null;
    }

    /// <summary>Builds a decision hitting every living combatant on the boss's own side (team buff/guard).</summary>
    protected static BossActionDecision? OnAllAllies(Combatant boss, string skillKey, Combat combat)
    {
        var targets = combat.Enemies.Where(e => !e.IsDefeated).ToArray();
        return targets.Length > 0 && Owns(boss, skillKey)
            ? new BossActionDecision(skillKey, targets.Select(e => e.Id.Value).ToArray())
            : null;
    }

    /// <summary>The fastest living player (Speed stat), tie-broken deterministically.</summary>
    protected static Combatant? FastestPlayer(Combat combat, Combatant boss)
        => LivingPlayers(combat)
            .OrderByDescending(a => a.EffectiveSpeed)
            .ThenBy(a => DeterministicTieKey(combat, boss, a))
            .FirstOrDefault();

    /// <summary>The most-wounded living combatant on the boss's own side, excluding the boss itself. Null if none other survive.</summary>
    protected static Combatant? MostWoundedAlly(Combat combat, Combatant boss)
        => combat.Enemies
            .Where(e => !e.IsDefeated && e.Id.Value != boss.Id.Value)
            .OrderBy(e => HpFraction(e))
            .ThenBy(e => DeterministicTieKey(combat, boss, e))
            .FirstOrDefault();

    /// <summary>True if the combatant currently carries an active negative StatModifier on the given stat.</summary>
    protected static bool HasNegativeStatModifier(Combatant combatant, CombatStat stat)
        => combatant.StatusEffects.Any(e =>
            e.Kind == StatusEffectKind.StatModifier && e.Stat == stat && e.Magnitude < 0);

    /// <summary>The slowest living player (Speed stat), tie-broken deterministically.</summary>
    protected static Combatant? SlowestPlayer(Combat combat, Combatant boss)
        => LivingPlayers(combat)
            .OrderBy(a => a.EffectiveSpeed)
            .ThenBy(a => DeterministicTieKey(combat, boss, a))
            .FirstOrDefault();

    /// <summary>The living player with the lowest EffectiveMagicDefense — the softest opener for a Magic-category mark/DoT.</summary>
    protected static Combatant? LowestMagicDefensePlayer(Combat combat, Combatant boss)
        => LivingPlayers(combat)
            .OrderBy(a => a.EffectiveMagicDefense)
            .ThenBy(a => DeterministicTieKey(combat, boss, a))
            .FirstOrDefault();

    /// <summary>The living player currently carrying a status effect with the given key (case-insensitive). Null if none.</summary>
    protected static Combatant? PlayerWithStatus(Combat combat, string statusKey)
        => LivingPlayers(combat).FirstOrDefault(p =>
            p.StatusEffects.Any(e => string.Equals(e.Key, statusKey, StringComparison.OrdinalIgnoreCase)));

    /// <summary>The living player NOT currently affected by any DamageOverTime effect. Null if everyone already is.</summary>
    protected static Combatant? PlayerWithoutDot(Combat combat, Combatant boss)
        => LivingPlayers(combat)
            .Where(p => p.StatusEffects.All(e => e.Kind != StatusEffectKind.DamageOverTime))
            .OrderBy(a => DeterministicTieKey(combat, boss, a))
            .FirstOrDefault();

    /// <summary>
    /// The living combatant on the boss's own side (excluding the boss) with the
    /// lowest current Mana, but only if that Mana is below <paramref name="threshold"/>.
    /// Null if no other ally survives or nobody is actually starved.
    /// </summary>
    protected static Combatant? MostManaStarvedAlly(Combat combat, Combatant boss, int threshold)
    {
        var candidate = combat.Enemies
            .Where(e => !e.IsDefeated && e.Id.Value != boss.Id.Value)
            .OrderBy(e => e.Mana)
            .ThenBy(e => DeterministicTieKey(combat, boss, e))
            .FirstOrDefault();

        return candidate is not null && candidate.Mana < threshold ? candidate : null;
    }

    /// <summary>
    /// Deterministic probability roll (true with the given probability, 0..1), seeded
    /// per combat/turn/boss/tag so combats replay identically — the same convention as
    /// <see cref="DeterministicTieKey"/>, but for weighted-choice AI branches (e.g.
    /// "55% Éclaboussure, else Corps de verre") instead of ordering ties.
    /// </summary>
    protected static bool Chance(Combat combat, Combatant boss, string tag, double probability)
        => DeterministicCombatRoll.UnitInterval(
            $"boss-chance:{tag}:{combat.Id.Value}:{combat.TurnNumber}:{boss.Id.Value}") < probability;
}

/// <summary>
/// L'Impératrice / la Vipère — venin puis curée. Empoisonne la cible la plus saine,
/// puis frappe ; sous 50 % PV, elle s'acharne sur le plus faible (curée).
/// </summary>
public sealed class ImperatriceVipereBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.imperatrice-vipere";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Phase enragée : curée sur le plus faible, chaque tour.
        if (HpFraction(boss) <= 0.50)
            return Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss));

        // Phase venin : un tour sur deux, empoisonne la cible la plus saine.
        return combat.TurnNumber % 2 == 0
            ? Strike(boss, "canon.skill.priere-aspiration", HighestHpPlayer(combat, boss))
                ?? Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss))
            : Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// Le Pape Louis XVII — édits réguliers (toutes les 3 actions) puis flamme froide
/// sur le plus faible. Quand il est très bas, il défère à l'IA générique (panique).
/// </summary>
public sealed class PapeLouisXviiBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.pape-louis-xvii";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Édit papal : une AoE-débuff toutes les trois actions (flavor + futur statut).
        if (combat.TurnNumber % 3 == 0 && Owns(boss, "canon.skill.brume"))
            return new BossActionDecision("canon.skill.brume",
                LivingPlayers(combat).Select(p => p.Id.Value).ToArray());

        return Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// L'Homoncule-roi — lent mais implacable. Se renforce au premier tour (transmutation),
/// puis martèle le plus faible ; sous 40 % PV, plus rien d'autre que la curée.
/// </summary>
public sealed class HomonculeRoiBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.homoncule-roi";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        if (HpFraction(boss) <= 0.40)
            return Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss));

        if (combat.TurnNumber == 1)
            return OnSelf(boss, "canon.skill.transmutation")
                ?? Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss));

        return Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// Le Grand Cardinal — surveillance : alterne aspiration (cible saine) et frappe.
/// </summary>
public sealed class GrandCardinalBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.grand-cardinal";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        return combat.TurnNumber % 2 == 0
            ? Strike(boss, "canon.skill.priere-aspiration", HighestHpPlayer(combat, boss))
                ?? Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss))
            : Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss));
    }
}

/// <summary>
/// Him'Lit — le démon lunaire. Cycle en trois temps (brume → aspiration → flamme
/// séraphine) tant qu'il domine ; sous 33 % PV, il déchaîne la flamme chaque tour.
/// </summary>
public sealed class HimLitBossBehavior : CanonBossBehaviorBase
{
    public override string BossKey => "canon.enemy.himlit";

    public override BossActionDecision? DecideAction(BossDecisionContext context)
    {
        var boss = context.Boss;
        var combat = context.Combat;

        // Apogée : sous 33 % PV, flamme séraphine sur le plus faible, sans répit.
        if (HpFraction(boss) <= 0.33)
            return Strike(boss, "canon.skill.flamme-seraphine", LowestHpPlayer(combat, boss))
                ?? Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss));

        return (combat.TurnNumber % 3) switch
        {
            0 when Owns(boss, "canon.skill.brume") => new BossActionDecision(
                "canon.skill.brume", LivingPlayers(combat).Select(p => p.Id.Value).ToArray()),
            1 => Strike(boss, "canon.skill.priere-aspiration", HighestHpPlayer(combat, boss))
                ?? Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss)),
            _ => Strike(boss, "canon.skill.flamme-seraphine", LowestHpPlayer(combat, boss))
                ?? Strike(boss, "canon.skill.flamme-froide", LowestHpPlayer(combat, boss))
        };
    }
}
