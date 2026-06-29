using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Ai;

/// <summary>
/// Utility-based enemy AI. For each affordable skill it builds candidate target
/// sets (following the same targeting convention as the resolver — from the enemy's
/// perspective, "SingleEnemy" hits the player party, "SingleAlly" hits the enemy's
/// own side) and scores the situation: finish wounded targets, focus-fire, heal hurt
/// allies, control healthy threats, defend when cornered. Category weights are tuned
/// per <see cref="Combatant.Archetype"/>. Ties break on a seeded deterministic roll.
/// </summary>
public sealed class UtilityEnemyActionPlanner : IEnemyActionPlanner
{
    public EnemyActionPlan? Plan(Combat combat, Combatant actor)
    {
        var weights = ArchetypeWeights.For(actor.Archetype);

        var playerSide = combat.Allies.Where(c => !c.IsDefeated).ToArray();
        var enemySide = combat.Enemies.Where(c => !c.IsDefeated).ToArray();

        EnemyActionPlan? best = null;

        foreach (var skill in actor.Skills)
        {
            if (!CanAfford(actor, skill))
            {
                continue;
            }

            var (targets, score) = Evaluate(combat, actor, skill, weights, playerSide, enemySide);

            if (targets.Count == 0)
            {
                continue;
            }

            // Deterministic tie-break jitter (never new Random()).
            var jitter = DeterministicCombatRoll.UnitInterval(
                $"enemy-ai:{combat.Id.Value}:{actor.Id.Value}:{combat.TurnNumber}:{combat.CurrentTick}:{skill.Key}") * 0.01;
            score += jitter;

            if (best is null || score > best.Score)
            {
                best = new EnemyActionPlan(skill, targets, score);
            }
        }

        return best;
    }

    private static bool CanAfford(Combatant actor, CombatantSkill skill)
        => skill.ManaCost <= actor.Mana && skill.ChargeCost <= actor.Charge;

    private static (IReadOnlyCollection<Guid> Targets, double Score) Evaluate(
        Combat combat,
        Combatant actor,
        CombatantSkill skill,
        ArchetypeWeights weights,
        IReadOnlyList<Combatant> playerSide,
        IReadOnlyList<Combatant> enemySide)
    {
        var category = Categorize(skill);

        return category switch
        {
            SkillCategory.OffenseSingle => ScoreOffenseSingle(combat, skill, weights, playerSide),
            SkillCategory.OffenseAll => ScoreOffenseAll(skill, weights, playerSide),
            SkillCategory.ControlSingle => ScoreControlSingle(combat, skill, weights, playerSide),
            SkillCategory.ControlAll => ScoreControlAll(skill, weights, playerSide),
            SkillCategory.Heal => ScoreHeal(skill, weights, enemySide),
            SkillCategory.Defense => ScoreDefense(actor, weights, playerSide),
            SkillCategory.Buff => ScoreBuff(actor, weights),
            _ => ([], double.NegativeInfinity)
        };
    }

    // ── Offense ───────────────────────────────────────────────────────────────

    private static (IReadOnlyCollection<Guid>, double) ScoreOffenseSingle(
        Combat combat, CombatantSkill skill, ArchetypeWeights w, IReadOnlyList<Combatant> playerSide)
    {
        if (playerSide.Count == 0)
            return ([], double.NegativeInfinity);

        Combatant? bestTarget = null;
        var bestSub = double.NegativeInfinity;

        foreach (var target in playerSide)
        {
            var sub = OffenseTargetScore(skill, target);
            if (sub > bestSub)
            {
                bestSub = sub;
                bestTarget = target;
            }
        }

        var score = (skill.BasePower + bestSub) * w.Offense;
        return ([bestTarget!.Id.Value], score);
    }

    private static (IReadOnlyCollection<Guid>, double) ScoreOffenseAll(
        CombatantSkill skill, ArchetypeWeights w, IReadOnlyList<Combatant> playerSide)
    {
        if (playerSide.Count == 0)
            return ([], double.NegativeInfinity);

        var sub = playerSide.Sum(t => OffenseTargetScore(skill, t));
        // AoE is worth more the more targets it hits.
        var score = (skill.BasePower * (0.6 + 0.4 * playerSide.Count) + sub) * w.Offense;
        return (playerSide.Select(t => t.Id.Value).ToArray(), score);
    }

    private static double OffenseTargetScore(CombatantSkill skill, Combatant target)
    {
        // Effective hit is approximated by the skill's power versus current guard.
        var effective = Math.Max(1, skill.BasePower - target.Guard);

        // Finisher: hugely prefer a target this hit can kill outright.
        if (effective >= target.CurrentVitality)
            return 120.0;

        // Otherwise focus-fire the most wounded target.
        var woundedFraction = target.MaxVitality > 0
            ? 1.0 - (double)target.CurrentVitality / target.MaxVitality
            : 0.0;
        return woundedFraction * 40.0;
    }

    // ── Control (debuff / status) ───────────────────────────────────────────────

    private static (IReadOnlyCollection<Guid>, double) ScoreControlSingle(
        Combat combat, CombatantSkill skill, ArchetypeWeights w, IReadOnlyList<Combatant> playerSide)
    {
        if (playerSide.Count == 0)
            return ([], double.NegativeInfinity);

        // Control is best spent on the healthiest threat (longest-lived target).
        var target = playerSide
            .OrderByDescending(t => t.CurrentVitality)
            .First();

        var score = (25.0 + (double)target.CurrentVitality / Math.Max(1, target.MaxVitality) * 15.0) * w.Control;
        return ([target.Id.Value], score);
    }

    private static (IReadOnlyCollection<Guid>, double) ScoreControlAll(
        CombatantSkill skill, ArchetypeWeights w, IReadOnlyList<Combatant> playerSide)
    {
        if (playerSide.Count == 0)
            return ([], double.NegativeInfinity);

        var score = (20.0 + 8.0 * playerSide.Count) * w.Control;
        return (playerSide.Select(t => t.Id.Value).ToArray(), score);
    }

    // ── Heal (own side) ──────────────────────────────────────────────────────────

    private static (IReadOnlyCollection<Guid>, double) ScoreHeal(
        CombatantSkill skill, ArchetypeWeights w, IReadOnlyList<Combatant> enemySide)
    {
        if (enemySide.Count == 0)
            return ([], double.NegativeInfinity);

        var target = enemySide
            .OrderBy(c => c.MaxVitality > 0 ? (double)c.CurrentVitality / c.MaxVitality : 1.0)
            .First();

        var woundedFraction = target.MaxVitality > 0
            ? 1.0 - (double)target.CurrentVitality / target.MaxVitality
            : 0.0;

        // Healing a near-full ally is near-worthless; a dying one is urgent.
        var score = woundedFraction * 90.0 * w.Support;
        return ([target.Id.Value], score);
    }

    // ── Defense / Buff (self) ─────────────────────────────────────────────────────

    private static (IReadOnlyCollection<Guid>, double) ScoreDefense(
        Combatant actor, ArchetypeWeights w, IReadOnlyList<Combatant> playerSide)
    {
        var dangerFraction = actor.MaxVitality > 0
            ? 1.0 - (double)actor.CurrentVitality / actor.MaxVitality
            : 0.0;
        var threatPressure = playerSide.Count * 0.08;

        var score = (dangerFraction * 55.0 + threatPressure * 20.0) * w.Defense;
        return ([actor.Id.Value], score);
    }

    private static (IReadOnlyCollection<Guid>, double) ScoreBuff(Combatant actor, ArchetypeWeights w)
    {
        // A self-buff is most valuable while healthy (you live to enjoy it).
        var healthFraction = actor.MaxVitality > 0
            ? (double)actor.CurrentVitality / actor.MaxVitality
            : 1.0;
        var score = (15.0 + healthFraction * 15.0) * w.Support;
        return ([actor.Id.Value], score);
    }

    // ── Categorisation ────────────────────────────────────────────────────────────

    private static SkillCategory Categorize(CombatantSkill skill)
    {
        var effect = skill.EffectType;
        var targeting = skill.TargetingType;

        if (IsHeal(skill))
            return SkillCategory.Heal;

        if (Eq(effect, "Guard") || Eq(skill.SkillType, "Defense"))
            return SkillCategory.Defense;

        if (Eq(effect, "Buff"))
            return SkillCategory.Buff;

        if (Eq(effect, "Debuff") || Eq(effect, "Status"))
            return Eq(targeting, "AllEnemies") ? SkillCategory.ControlAll : SkillCategory.ControlSingle;

        // Default: treat as offense (Damage / DamageVitality / anything else aimed at the party).
        return Eq(targeting, "AllEnemies") ? SkillCategory.OffenseAll : SkillCategory.OffenseSingle;
    }

    private static bool IsHeal(CombatantSkill skill)
        => Eq(skill.EffectType, "Heal") || Eq(skill.SkillType, "Heal");

    private static bool Eq(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private enum SkillCategory
    {
        OffenseSingle,
        OffenseAll,
        ControlSingle,
        ControlAll,
        Heal,
        Defense,
        Buff
    }

    private readonly record struct ArchetypeWeights(double Offense, double Control, double Support, double Defense)
    {
        public static ArchetypeWeights For(string archetype) => archetype?.Trim().ToLowerInvariant() switch
        {
            "rupture" => new(1.4, 0.8, 0.6, 0.7),   // burst aggressor
            "shadow" => new(1.2, 1.1, 0.8, 0.8),    // drain / harasser
            "memory" => new(0.9, 1.4, 1.0, 1.0),    // controller
            "fragile" => new(1.1, 1.0, 1.0, 1.3),   // self-preserving
            "trauma" => new(1.0, 1.0, 0.9, 1.0),    // balanced
            "boss" => new(1.2, 1.2, 1.1, 1.0),      // adaptive
            "elite" => new(1.2, 1.1, 1.0, 0.9),
            _ => new(1.0, 1.0, 1.0, 1.0)
        };
    }
}
