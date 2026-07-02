using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.EnemyTurns.Ai;

public sealed class UtilityEnemyActionPlanner : IEnemyActionPlanner
{
    public EnemyActionPlan? Plan(Combat combat, Combatant actor)
    {
        var weights = ArchetypeWeights.For(actor.Archetype);
        var players = combat.Allies.Where(c => !c.IsDefeated).ToArray();
        var team = combat.Enemies.Where(c => !c.IsDefeated).ToArray();

        EnemyActionPlan? best = null;
        foreach (var skill in actor.Skills)
        {
            if (!CanAfford(actor, skill)) continue;

            var targets = ResolveTargets(combat, actor, skill, players, team);
            if (targets.Count == 0) continue;

            var score = ScoreSkill(combat, actor, skill, targets, weights, players, team);
            var jitter = DeterministicCombatRoll.UnitInterval(
                $"enemy-ai:{combat.Id.Value}:{actor.Id.Value}:{combat.TurnNumber}:{combat.CurrentTick}:{skill.Key}") * 0.01;
            score += jitter;

            if (best is null || score > best.Score)
                best = new EnemyActionPlan(skill, targets, score);
        }
        return best;
    }

    private static bool CanAfford(Combatant actor, CombatantSkill skill)
        => skill.ManaCost <= actor.Mana && skill.ChargeCost <= actor.Charge;

    private static IReadOnlyCollection<Guid> ResolveTargets(
        Combat combat, Combatant actor, CombatantSkill skill, IReadOnlyList<Combatant> players, IReadOnlyList<Combatant> team)
    {
        switch ((skill.TargetingType ?? string.Empty).Trim())
        {
            case "Self": return [actor.Id.Value];
            case "AllEnemies": return players.Select(p => p.Id.Value).ToArray();
            case "AllAllies": return team.Select(t => t.Id.Value).ToArray();
            case "SingleAlly":
                {
                    var ally = team.OrderBy(HpFraction).FirstOrDefault() ?? actor;
                    return [ally.Id.Value];
                }
            case "SingleEnemy":
            case "AnySingle":
            default:
                {
                    var target = PickOffenseTarget(combat, actor, skill, players);
                    return target is null ? [] : [target.Id.Value];
                }
        }
    }

    /// <summary>
    /// Picks the best single offense target among living players. Scoring blends
    /// "can this hit kill them" / how wounded they are with accrued threat and
    /// whether they're this actor's own last attacker, then breaks any remaining
    /// tie with a deterministic per-candidate hash (never array position) — this
    /// is what stops enemies from always defaulting to the first ally (the
    /// protagonist) when nobody's hurt or threatening yet.
    /// </summary>
    private static Combatant? PickOffenseTarget(
        Combat combat, Combatant actor, CombatantSkill skill, IReadOnlyList<Combatant> players)
    {
        Combatant? best = null;
        var bestScore = double.NegativeInfinity;
        foreach (var target in players)
        {
            var s = OffenseTargetScore(combat, actor, skill, target, players);
            if (s > bestScore) { bestScore = s; best = target; }
        }
        return best;
    }

    private static double ScoreSkill(
        Combat combat, Combatant actor, CombatantSkill skill, IReadOnlyCollection<Guid> targets,
        ArchetypeWeights w, IReadOnlyList<Combatant> players, IReadOnlyList<Combatant> team)
    {
        switch (Categorize(skill))
        {
            case SkillCategory.Heal:
                {
                    var healed = team.Where(t => targets.Contains(t.Id.Value)).ToArray();
                    var wounded = healed.Length > 0 ? healed.Max(WoundedFraction) : 0.0;
                    return wounded * 90.0 * w.Support;
                }
            case SkillCategory.Defense:
                return DangerFactor(actor, players) * 55.0 * w.Defense;
            case SkillCategory.Buff:
                return (15.0 + HpFraction(actor) * 15.0) * w.Support;
            case SkillCategory.Control:
                {
                    var hit = players.Where(p => targets.Contains(p.Id.Value)).ToArray();
                    var healthiest = hit.Length > 0 ? hit.Max(HpFraction) : 0.0;
                    return (25.0 + healthiest * 15.0 + 6.0 * Math.Max(0, hit.Length - 1)) * w.Control;
                }
            default:
                {
                    var hit = players.Where(p => targets.Contains(p.Id.Value)).ToArray();
                    var sub = hit.Sum(p => OffenseTargetScore(combat, actor, skill, p, players));
                    var aoe = hit.Length > 1 ? skill.BasePower * 0.4 * hit.Length : 0.0;
                    return (skill.BasePower + sub + aoe) * w.Offense;
                }
        }
    }

    /// <summary>
    /// Scores one candidate target for an offensive skill: base lethal/wounded
    /// logic, plus how much threat they've drawn relative to the other living
    /// candidates, plus a bonus if they're this actor's own last attacker, plus
    /// a deterministic per-candidate jitter that breaks exact ties without ever
    /// favoring array position (see <see cref="PickOffenseTarget"/>).
    /// </summary>
    private static double OffenseTargetScore(
        Combat combat, Combatant actor, CombatantSkill skill, Combatant target, IReadOnlyList<Combatant> candidates)
    {
        var effective = Math.Max(1, skill.BasePower - target.Guard);
        var baseScore = effective >= target.CurrentVitality ? 120.0 : WoundedFraction(target) * 40.0;

        var maxThreat = candidates.Count > 0 ? candidates.Max(c => c.ThreatValue) : 0.0;
        var threatScore = maxThreat > 0
            ? (target.ThreatValue / maxThreat) * ThreatTuning.TargetThreatWeight
            : 0.0;

        var stickyScore = actor.LastAttackerId == target.Id.Value
            ? ThreatTuning.TargetLastAttackerBonus
            : 0.0;

        var jitter = DeterministicCombatRoll.UnitInterval(
            $"enemy-ai-target:{combat.Id.Value}:{combat.TurnNumber}:{combat.CurrentTick}:{actor.Id.Value}:{skill.Key}:{target.Id.Value}")
            * ThreatTuning.TargetJitterWeight;

        return baseScore + threatScore + stickyScore + jitter;
    }

    private static double HpFraction(Combatant c) => c.MaxVitality > 0 ? (double)c.CurrentVitality / c.MaxVitality : 0.0;
    private static double WoundedFraction(Combatant c) => 1.0 - HpFraction(c);
    private static double DangerFactor(Combatant actor, IReadOnlyList<Combatant> players)
        => WoundedFraction(actor) + players.Count * 0.08;

    private static SkillCategory Categorize(CombatantSkill skill)
    {
        if (Eq(skill.EffectType, "Heal") || Eq(skill.SkillType, "Heal")) return SkillCategory.Heal;
        if (Eq(skill.EffectType, "Guard") || Eq(skill.SkillType, "Defense")) return SkillCategory.Defense;
        if (Eq(skill.EffectType, "Buff")) return SkillCategory.Buff;
        if (Eq(skill.EffectType, "Debuff") || Eq(skill.EffectType, "Status")) return SkillCategory.Control;
        return SkillCategory.Offense;
    }

    private static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private enum SkillCategory { Offense, Control, Heal, Defense, Buff }

    private readonly record struct ArchetypeWeights(double Offense, double Control, double Support, double Defense)
    {
        public static ArchetypeWeights For(string archetype) => (archetype ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "rupture" => new(1.4, 0.8, 0.6, 0.7),
            "shadow" => new(1.2, 1.1, 0.8, 0.8),
            "memory" => new(0.9, 1.4, 1.0, 1.0),
            "fragile" => new(1.1, 1.0, 1.0, 1.3),
            "trauma" => new(1.0, 1.0, 0.9, 1.0),
            "boss" => new(1.2, 1.2, 1.1, 1.0),
            "elite" => new(1.2, 1.1, 1.0, 0.9),
            _ => new(1.0, 1.0, 1.0, 1.0)
        };
    }
}