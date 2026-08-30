using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Application.Combats.EncounterDrafts;

/// <summary>
/// Validates the complete Catalog/run contract before any combatant is created.
/// A malformed definition is rejected with its source key instead of being repaired
/// with tactical defaults inside <see cref="CombatFactory"/>.
/// </summary>
public static class CombatEncounterDraftValidator
{
    public static void Validate(CombatEncounterDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (draft.RunId == Guid.Empty) throw new DomainException("Combat draft run id is required.");
        if (draft.RoomId == Guid.Empty) throw new DomainException("Combat draft room id is required.");
        if (draft.NodeId == Guid.Empty) throw new DomainException("Combat draft node id is required.");
        if (string.IsNullOrWhiteSpace(draft.RoomType)) throw new DomainException("Combat draft room type is required.");
        if (string.IsNullOrWhiteSpace(draft.EncounterType)) throw new DomainException("Combat draft encounter type is required.");
        if (draft.RiskLevel < 0) throw new DomainException("Combat draft risk level cannot be negative.");
        if (draft.DifficultyMultiplier <= 0) throw new DomainException("Combat draft difficulty multiplier must be positive.");
        if (draft.Allies.Count == 0) throw new DomainException("Combat draft must contain at least one ally.");
        if (draft.Enemies.Count == 0) throw new DomainException("Combat draft must contain at least one enemy.");

        var duplicateCharacterIds = draft.Allies
            .Where(ally => ally.CharacterInstanceId is not null)
            .GroupBy(ally => ally.CharacterInstanceId!.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateCharacterIds.Length > 0)
            throw new DomainException(
                $"Combat draft contains duplicate character instance ids: {string.Join(", ", duplicateCharacterIds)}.");

        foreach (var ally in draft.Allies)
        {
            Require(ally.AllyKey, "Ally definition key");
            Require(ally.DisplayName, $"Ally '{ally.AllyKey}' display name");
            Require(ally.Role, $"Ally '{ally.AllyKey}' role");
            EmotionalTypeCode.ParseRequired(ally.EmotionalRegister, $"Ally '{ally.AllyKey}' emotional register");

            if (ally.CharacterInstanceId is null || ally.CharacterInstanceId == Guid.Empty)
                throw new DomainException($"Ally '{ally.AllyKey}' character instance id is required.");

            if (!ally.IsProtagonist)
            {
                if (ally.MaxVitality <= 0)
                    throw new DomainException($"Ally '{ally.AllyKey}' max vitality must be positive.");
                if (ally.Movement < 1)
                    throw new DomainException($"Ally '{ally.AllyKey}' movement must be at least one.");
            }
            if (ally.AttackPower < 0 || ally.Defense < 0 || ally.StartingGuard < 0
                || ally.Speed < 1 || ally.Focus < 0 || ally.Mana < 0 || ally.Charge < 0
                || ally.MagicAttack < 0 || ally.MagicDefense < 0 || ally.Movement < 1)
                throw new DomainException($"Ally '{ally.AllyKey}' contains invalid combat statistics.");

            ValidateSkills(ally.AllyKey, ally.Skills ?? []);
        }

        foreach (var enemy in draft.Enemies)
        {
            Require(enemy.EnemyKey, "Enemy definition key");
            Require(enemy.DisplayName, $"Enemy '{enemy.EnemyKey}' display name");
            Require(enemy.Archetype, $"Enemy '{enemy.EnemyKey}' archetype");
            EmotionalTypeCode.ParseRequired(enemy.EmotionalRegister, $"Enemy '{enemy.EnemyKey}' emotional register");

            if (enemy.BaseDifficulty < 0)
                throw new DomainException($"Enemy '{enemy.EnemyKey}' base difficulty cannot be negative.");
            if (enemy.MinRiskLevel > enemy.MaxRiskLevel)
                throw new DomainException($"Enemy '{enemy.EnemyKey}' risk range is invalid.");
            if (enemy.Speed < 1)
                throw new DomainException($"Enemy '{enemy.EnemyKey}' speed must be at least one.");
            if (enemy.Movement < 1)
                throw new DomainException($"Enemy '{enemy.EnemyKey}' movement must be at least one.");
            if (enemy.AttackPower < 0 || enemy.Defense < 0 || enemy.Focus < 0
                || enemy.MagicAttack < 0 || enemy.MagicDefense < 0 || enemy.Mana < 0)
                throw new DomainException($"Enemy '{enemy.EnemyKey}' contains invalid combat statistics.");

            var duplicateDeclaredSkills = DuplicateKeys(enemy.SkillKeys);
            if (duplicateDeclaredSkills.Length > 0)
                throw new DomainException(
                    $"Enemy '{enemy.EnemyKey}' declares duplicate skills: " +
                    string.Join(", ", duplicateDeclaredSkills) + ".");

            var declaredKeys = enemy.SkillKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var resolvedKeys = enemy.Skills.Select(skill => skill.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unresolved = declaredKeys.Except(resolvedKeys, StringComparer.OrdinalIgnoreCase).ToArray();
            if (unresolved.Length > 0)
                throw new DomainException(
                    $"Enemy '{enemy.EnemyKey}' has unresolved skills: {string.Join(", ", unresolved)}.");

            ValidateSkills(enemy.EnemyKey, enemy.Skills);
        }
    }

    private static void ValidateSkills(string ownerKey, IReadOnlyCollection<CombatEncounterDraftSkill> skills)
    {
        var duplicates = DuplicateKeys(skills.Select(skill => skill.Key));
        if (duplicates.Length > 0)
            throw new DomainException($"'{ownerKey}' contains duplicate skills: {string.Join(", ", duplicates)}.");

        foreach (var skill in skills)
        {
            Require(skill.Key, $"Skill key on '{ownerKey}'");
            Require(skill.DisplayName, $"Skill '{skill.Key}' display name");
            Require(skill.SkillType, $"Skill '{skill.Key}' type");
            Require(skill.TargetingType, $"Skill '{skill.Key}' targeting type");
            Require(skill.EffectType, $"Skill '{skill.Key}' effect type");
            EmotionalTypeCode.ParseRequired(skill.EmotionalRegister, $"Skill '{skill.Key}' emotional register");

            if (skill.ManaCost < 0 || skill.ChargeCost < 0 || skill.BasePower < 0 || skill.Cooldown < 0)
                throw new DomainException($"Skill '{skill.Key}' contains a negative cost, power or cooldown.");
            if (skill.TacticalRange < 0)
                throw new DomainException($"Skill '{skill.Key}' tactical range cannot be negative.");
            if (skill.Category is not ("Physical" or "Magic"))
                throw new DomainException($"Skill '{skill.Key}' category must be Physical or Magic.");
            if (skill.TacticalAreaShape is not ("Single" or "Cross" or "Diamond" or "Map"))
                throw new DomainException($"Skill '{skill.Key}' tactical area shape is invalid.");
        }
    }

    private static string[] DuplicateKeys(IEnumerable<string> keys) => keys
        .Where(key => !string.IsNullOrWhiteSpace(key))
        .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
        .Where(group => group.Count() > 1)
        .Select(group => group.Key)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static void Require(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");
    }
}
