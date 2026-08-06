using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats;

public sealed class CombatFactory : ICombatFactory
{
    private const int EnemyVitalityBase = 40;
    private const int VitalityPerDifficulty = 50;

    /// <summary>"Loi du Reflet" — the mirrored enemy copy's stats, as a fraction of the original.</summary>
    private const double MirrorCombatCopyStatMultiplier = 0.6;

    /// <summary>Base Mana design rule: every combatant's Mana is 85% of its (scaled)
    /// MaxVitality — see PlayerCharacterStatBlock.CreateDefaultPorteur for the
    /// player/companion side of the same rule.</summary>
    private const double EnemyManaToVitalityRatio = 0.85;

    private readonly EnemyStatScaler _enemyStatScaler = new();

    private static (double VitalityMultiplier, double PowerMultiplier, int GuardBonus) EncounterBonus(string encounterType)
    {
        return encounterType switch
        {
            "RoomBoss" => (2.6, 1.6, 18),
            "Elite" => (1.7, 1.3, 8),
            "Rare" => (1.4, 1.25, 4),
            _ => (1.0, 1.0, 0)
        };
    }


    /// <summary>
    /// Constitue les deux camps sans décider de la façon dont ils s'affronteront. C'est le
    /// travail réel de cette fabrique : mise à l'échelle des stats, résolution des compétences,
    /// application des Lois du Palais. Le combat tactique place ensuite ce roster sur la grille.
    /// </summary>
    public CombatRoster BuildRoster(
        CombatId combatId,
        CombatEncounterDraft draft,
        PlayerRuntimeState? playerState = null,
        IReadOnlyCollection<RunModifier>? runModifiers = null,
        int attackPower = 0,
        int defense = 0,
        int speed = 10,
        PalaceRoomState palaceRoomState = PalaceRoomState.Neutral,
        int focus = 0,
        IReadOnlyDictionary<string, IReadOnlyList<SkillStatusEffectSpec>>? skillEffects = null,
        IReadOnlyDictionary<EmotionalType, int>? typedDamageReductions = null,
        int hitChanceBonusPercent = 0,
        int dotDurationReductionPercent = 0,
        int dotDamageReductionPercent = 0,
        int dotDamageBonusPercent = 0,
        int magicDamageBonusPercent = 0,
        int magicDamageReductionPercent = 0,
        int criticalChanceBonusPercent = 0,
        int guardBonusPercent = 0,
        bool himLitProtectionEnabled = false,
        int healingBonusPercent = 0,
        int magicAttack = 0,
        int magicDefense = 0,
        string? forgottenSkillKey = null,
        string? roomTheme = null,
        IReadOnlyCollection<CatalogItemEquipmentEffect>? conditionalEquipmentEffects = null)
    {
        // Sum all unconsumed StartingGuardBonus modifiers (e.g. Éclat de garde: +8 garde).
        var guardBonus = runModifiers?
            .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
            .Sum(m => (int)m.Value) ?? 0;
        var activeModifiers = runModifiers ?? [];
        var attackPowerMultiplier = 1.0 + activeModifiers
            .Where(m => m.Type == RunModifierType.AttackPowerBonus && !m.IsConsumed)
            .Sum(m => m.Value);
        // Team-wide Speed multiplier (e.g. Rêve d'Erina: +5% team Speed while carried) —
        // applied to every Player-side combatant (protagonist AND companions), unlike
        // AttackPowerBonus which only scales skill power.
        var speedMultiplier = 1.0 + activeModifiers
            .Where(m => m.Type == RunModifierType.SpeedBonus && !m.IsConsumed)
            .Sum(m => m.Value);
        // "Loi de l'Impôt du Seuil" (law.impot-seuil) insolvency penalty: stacking -2%
        // team max HP per unpaid room toll this floor. Applied to every Player-side
        // combatant (protagonist AND companions) — "l'équipe" in the SFD text.
        var maxVitalityMultiplier = 1.0 - activeModifiers
            .Where(m => m.Type == RunModifierType.MaxHpReductionPercent && !m.IsConsumed)
            .Sum(m => m.Value) / 100.0;
        var activeClimate = ResolveActiveClimate(draft.RoomId, activeModifiers);

        // "Loi des Visites Terminées" (law.visites-terminees, liée à room.hopital): "les
        // sorts de soin sont sans effet dans cette salle" — a room-bound rule (RoomKey),
        // not a RunModifier/tirage, always active whenever combat happens in that room.
        var healingBlocked = string.Equals(draft.RoomKey, "room.hopital", StringComparison.OrdinalIgnoreCase);

        // "Loi de la Falaise" (law.falaise, liée à room.falaise): "10% de chance qu'un
        // combattant aléatoire soit repoussé d'un rang" each turn — same room-bound
        // convention as healingBlocked above, resolved by Combat.AdvanceTurn.
        var falaiseWindEnabled = string.Equals(draft.RoomKey, "room.falaise", StringComparison.OrdinalIgnoreCase);

        // "Loi de l'Éloge Funèbre" (law.eloge-funebre): a normal ambient-drawn law (not
        // room-bound), baked in from the run's active RunModifiers.
        var postDeathBasicAttackOnlyEnabled = activeModifiers
            .Any(m => m.Type == RunModifierType.PostDeathBasicAttackOnly && !m.IsConsumed);

        // "Loi du Tapis Propre" (law.tapis-propre): same ambient-drawn convention.
        var tapisPropreEnabled = activeModifiers
            .Any(m => m.Type == RunModifierType.TapisPropreEnabled && !m.IsConsumed);

        // "Loi de la Troisième Tasse" (law.troisieme-tasse): same ambient-drawn convention.
        var thirdCupHealCorruptionEnabled = activeModifiers
            .Any(m => m.Type == RunModifierType.ThirdCupHealCorruptionEnabled && !m.IsConsumed);

        // "Loi des Présentations" (law.presentations): same ambient-drawn convention.
        var presentationsEnabled = activeModifiers
            .Any(m => m.Type == RunModifierType.PresentationsEnabled && !m.IsConsumed);

        // "Loi du Miroir" (law.miroir): same ambient-drawn convention.
        var miroirEnabled = activeModifiers
            .Any(m => m.Type == RunModifierType.MiroirEnabled && !m.IsConsumed);

        if (activeClimate == RoomClimate.Rain)
        {
            guardBonus += 5;
        }

        // "Loi de la Marée Haute" (law.maree-haute, Pluie violacée): "+1 dégât par tour à
        // tous les DoT" — baked into Combat at creation, consumed by
        // CombatSkillEffectResolver.ApplyStatusEffectSpec for every newly-applied DoT.
        var dotMagnitudeBonus = 0;

        // Equipment-driven percentage bonus on top of the Law/climate-derived guard
        // above (e.g. Bague de Iris: +20% — 0 guard stays 0, 100 becomes 120).
        guardBonus = (int)Math.Round(guardBonus * (1.0 + guardBonusPercent / 100.0));

        // Item-driven emotional attack type for the player character (e.g. a mask
        // that turns the hero's attacks to Rupture). Null when no override is active.
        var attackTypeOverride = ResolveAttackTypeOverride(activeModifiers);

        var allies = draft.Allies
            .Select(ally =>
            {
                if (ally.IsProtagonist)
                {
                    // Protagonist: current HP/mana/charge from PlayerState, run stats,
                    // PlayerState skills, and the item-driven attack-type override.
                    var protagonistSkills = playerState?.Skills
                        .Select(s => CombatantSkill.Create(
                            s.Key,
                            s.DisplayName,
                            s.SkillType,
                            s.TargetingType,
                            NormalizeCombatEffectType(s.Key, s.EffectType),
                            s.ManaCost,
                            s.ChargeCost,
                            ScalePlayerSkillPower(s.EffectType, s.BasePower, attackPowerMultiplier),
                            statusEffects: EffectFor(skillEffects, s.Key),
                            category: s.Category,
                            basePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality,
                            tacticalRange: s.TacticalRange,
                            tacticalAreaShape: ParseTacticalAreaShape(s.TacticalAreaShape),
                            requiresLineOfSight: s.RequiresLineOfSight,
                            cooldown: s.Cooldown,
                            isUltimate: s.IsUltimate,
                            emotionalRegister: s.EmotionalRegister))
                        .ToArray()
                        ?? GetDefaultAllySkills(attackPowerMultiplier, skillEffects);

                    var maxVitality = Math.Max(1,
                        (int)Math.Round((playerState?.MaxVitality ?? 100) * maxVitalityMultiplier));
                    var currentVitality = Math.Min(playerState?.CurrentVitality ?? maxVitality, maxVitality);
                    var guard = (playerState?.Guard ?? 0) + guardBonus;
                    var mana = playerState?.Mana ?? 0;
                    var maxMana = playerState?.MaxMana;
                    var charge = playerState?.Charge ?? 0;

                    var protagonist = Combatant.Create(
                        CombatantId.New(),
                        ally.AllyKey,
                        ally.DisplayName,
                        CombatantSide.Player,
                        ally.Role,
                        maxVitality,
                        currentVitality,
                        guard,
                        baseGuard: guardBonus,
                        mana,
                        charge,
                        protagonistSkills,
                        attackPower: attackPower,
                        defense: defense,
                        speed: ApplySpeedMultiplier(speed, speedMultiplier),
                        focus: focus,
                        maxMana: maxMana,
                        magicAttack: magicAttack,
                        magicDefense: magicDefense,
                        naturalEmotionalType: EmotionalTypeCode.ParseRequired(
                            ally.EmotionalRegister,
                            $"Ally '{ally.AllyKey}' natural emotional register"));

                    protagonist.ApplyAttackTypeOverride(attackTypeOverride);
                    protagonist.ApplyTypedDamageReductions(typedDamageReductions);
                    protagonist.ApplyEquipmentCombatModifiers(
                        hitChanceBonusPercent, dotDurationReductionPercent, dotDamageReductionPercent,
                        magicDamageBonusPercent, magicDamageReductionPercent, criticalChanceBonusPercent,
                        dotDamageBonusPercent, healingBonusPercent);

                    if (himLitProtectionEnabled)
                    {
                        ApplyHimLitProtection(protagonist);
                    }

                    return protagonist;
                }

                // Companion: its OWN kit and stats; starts the fight at full vitality.
                var companionSkills = ally.Skills is { Count: > 0 }
                    ? ally.Skills
                        .Select(s => CombatantSkill.Create(
                            s.Key,
                            s.DisplayName,
                            s.SkillType,
                            s.TargetingType,
                            NormalizeCombatEffectType(s.Key, s.EffectType),
                            s.ManaCost,
                            s.ChargeCost,
                            ScalePlayerSkillPower(s.EffectType, s.BasePower, attackPowerMultiplier),
                            statusEffects: EffectFor(skillEffects, s.Key),
                            category: s.Category,
                            basePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality,
                            tacticalRange: s.TacticalRange,
                            tacticalAreaShape: ParseTacticalAreaShape(s.TacticalAreaShape),
                            requiresLineOfSight: s.RequiresLineOfSight,
                            cooldown: s.Cooldown,
                            isUltimate: s.IsUltimate,
                            emotionalRegister: s.EmotionalRegister))
                        .ToArray()
                    : GetDefaultAllySkills(attackPowerMultiplier, skillEffects);

                var companionMaxVitality = Math.Max(1, (int)Math.Round(
                    (ally.MaxVitality > 0 ? ally.MaxVitality : 100) * maxVitalityMultiplier));

                var companion = Combatant.Create(
                    CombatantId.New(),
                    ally.AllyKey,
                    ally.DisplayName,
                    CombatantSide.Player,
                    ally.Role,
                    companionMaxVitality,
                    currentVitality: companionMaxVitality,
                    guard: ally.StartingGuard + guardBonus,
                    baseGuard: guardBonus,
                    mana: ally.Mana,
                    charge: ally.Charge,
                    companionSkills,
                    attackPower: ally.AttackPower,
                    defense: ally.Defense,
                    speed: ApplySpeedMultiplier(ally.Speed, speedMultiplier),
                    focus: ally.Focus,
                    maxMana: ally.Mana,
                    magicAttack: ally.MagicAttack,
                    magicDefense: ally.MagicDefense,
                    movement: ally.Movement,
                    naturalEmotionalType: EmotionalTypeCode.ParseRequired(
                        ally.EmotionalRegister,
                        $"Ally '{ally.AllyKey}' natural emotional register"));

                // Companions keep their own emotional type (no item override).
                companion.ApplyAttackTypeOverride(null);
                return companion;
            })
            .ToArray();

        // Room/weather-conditional equipment bonuses (e.g. Boussole du Pèlerin) only ever
        // target the protagonist's own equipment, never companions.
        var protagonistAllyKey = draft.Allies.FirstOrDefault(a => a.IsProtagonist)?.AllyKey;
        var protagonistCombatant = protagonistAllyKey is null
            ? null
            : allies.FirstOrDefault(a => a.SourceKey == protagonistAllyKey);
        ApplyConditionalEquipmentStatBundle(
            protagonistCombatant, roomTheme, activeClimate, conditionalEquipmentEffects ?? []);

        var (bossVitalityMultiplier, bossPowerMultiplier, bossGuardBonus) = EncounterBonus(draft.EncounterType);

        // "Loi du Reflet" (law.reflet): "reflète votre propre équipe dans le camp
        // adverse" — the next combat's entire enemy side is replaced by a mirror of
        // the PLAYER's own team (same skills, 60% stats), not by clones of the room's
        // actual enemies. AI copying "vos trois derniers combats" is simplified to the
        // mirrored combatant's own (i.e. the ally's) default skill rotation, since no
        // combat-history tracking exists (documented simplification).
        var isMirrorCombatCopyActive = activeModifiers.Any(m => m.Type == RunModifierType.MirrorCombatCopy && !m.IsConsumed);

        if (isMirrorCombatCopyActive)
        {
            var mirroredEnemies = allies
                .Select(ally => Combatant.Create(
                    CombatantId.New(),
                    sourceKey: $"reflet.{ally.SourceKey}",
                    displayName: $"Reflet de {ally.DisplayName}",
                    side: CombatantSide.Enemy,
                    archetype: ally.Archetype,
                    maxVitality: Math.Max(1, (int)Math.Round(ally.MaxVitality * MirrorCombatCopyStatMultiplier)),
                    currentVitality: Math.Max(1, (int)Math.Round(ally.MaxVitality * MirrorCombatCopyStatMultiplier)),
                    guard: 0,
                    baseGuard: 0,
                    mana: ally.Mana,
                    charge: 0,
                    skills: ally.Skills,
                    attackPower: (int)Math.Round(ally.BaseStatSnapshot.AttackPower * MirrorCombatCopyStatMultiplier),
                    defense: (int)Math.Round(ally.BaseStatSnapshot.Defense * MirrorCombatCopyStatMultiplier),
                    speed: Math.Max(1, (int)Math.Round(ally.BaseStatSnapshot.Speed * MirrorCombatCopyStatMultiplier)),
                    focus: (int)Math.Round(ally.BaseStatSnapshot.Focus * MirrorCombatCopyStatMultiplier),
                    maxMana: ally.MaxMana,
                    magicAttack: (int)Math.Round(ally.BaseStatSnapshot.MagicAttack * MirrorCombatCopyStatMultiplier),
                    magicDefense: (int)Math.Round(ally.BaseStatSnapshot.MagicDefense * MirrorCombatCopyStatMultiplier),
                    naturalEmotionalType: ally.NaturalEmotionalType))
                .ToArray();

            if (activeModifiers.Any(m => m.Type == RunModifierType.TurnOrderReverse && !m.IsConsumed))
            {
                ApplyTurnOrderReversal(allies.Concat(mirroredEnemies));
            }

            if (activeModifiers.Any(m => m.Type == RunModifierType.TurnOrderLock && !m.IsConsumed))
            {
                ApplyStrictInitiativeOrder(allies.Concat(mirroredEnemies));
            }

            if (activeModifiers.Any(m => m.Type == RunModifierType.CruelDestinyForEveryone && !m.IsConsumed))
            {
                ApplyCruelDestinyToEveryone(allies.Concat(mirroredEnemies));
            }

            ApplyClimateStatBundle(activeClimate, allies.Concat(mirroredEnemies));
            ApplyAllyHealingBonus(activeModifiers, allies);
            ApplySilenceDuBundle(activeModifiers, allies.Concat(mirroredEnemies));

            var mirrorHitCounterDoubleDamageEnabled = activeModifiers
                .Any(m => m.Type == RunModifierType.HitCounterDoubleDamage && !m.IsConsumed);
            var mirrorFirstHitCriticalEnabled = activeModifiers
                .Any(m => m.Type == RunModifierType.FirstHitCritical && !m.IsConsumed);
            var mirrorLowHpDamageAmplificationEnabled = activeModifiers
                .Any(m => m.Type == RunModifierType.DamageAmplificationBelowHpThreshold && !m.IsConsumed);

            return new CombatRoster(
                allies,
                mirroredEnemies,
                mirrorHitCounterDoubleDamageEnabled,
                mirrorFirstHitCriticalEnabled,
                mirrorLowHpDamageAmplificationEnabled,
                ComputeDotDurationExtensionTicks(activeModifiers),
                ComputeDuelDamageAsymmetryEnabled(activeModifiers),
                dotMagnitudeBonus,
                healingBlocked,
                falaiseWindEnabled,
                postDeathBasicAttackOnlyEnabled,
                tapisPropreEnabled,
                thirdCupHealCorruptionEnabled,
                presentationsEnabled,
                miroirEnabled,
                forgottenSkillKey);
        }

        var enemies = draft.Enemies
            .SelectMany(enemy =>
            {
                var baseVitality = (int)Math.Ceiling(
                    (EnemyVitalityBase + enemy.BaseDifficulty * VitalityPerDifficulty) * bossVitalityMultiplier); var representativePower = enemy.Skills.Count > 0
                    ? enemy.Skills.Max(s => s.BasePower)
                    : 0;

                var scaled = _enemyStatScaler.Scale(baseVitality, representativePower, draft.DifficultyMultiplier);
                var enemyPowerMultiplier = (activeClimate switch
                {
                    RoomClimate.Grey => 0.90,
                    RoomClimate.Heatwave => 1.10,
                    _ => 1.0
                }) * bossPowerMultiplier;
                var enemyStartingGuard = (palaceRoomState == PalaceRoomState.Silent ? 8 : 0) + bossGuardBonus;

                // Attack/Defense/Focus/Speed all keep pace with run depth and active
                // Palace Laws the same way Vitality/Power do, via the same difficulty
                // multiplier, so enemy stats genuinely scale as a percentage increase.
                var scaledAttackPower = _enemyStatScaler.ScaleValue(enemy.AttackPower, draft.DifficultyMultiplier);
                var scaledDefense = _enemyStatScaler.ScaleValue(enemy.Defense, draft.DifficultyMultiplier);
                var scaledFocus = _enemyStatScaler.ScaleValue(enemy.Focus, draft.DifficultyMultiplier);
                var scaledSpeed = _enemyStatScaler.ScaleValue(enemy.Speed, draft.DifficultyMultiplier);
                var scaledMagicAttack = _enemyStatScaler.ScaleValue(enemy.MagicAttack, draft.DifficultyMultiplier);
                var scaledMagicDefense = _enemyStatScaler.ScaleValue(enemy.MagicDefense, draft.DifficultyMultiplier);

                var skills = enemy.Skills
                    .Select(s =>
                    {
                        var scaledSkill = _enemyStatScaler.Scale(baseVitality, s.BasePower, draft.DifficultyMultiplier);
                        var power = ScaleEnemySkillPower(
                            s.EffectType,
                            scaledSkill.Power,
                            enemyPowerMultiplier,
                            palaceRoomState);

                        return CombatantSkill.Create(
                            s.Key,
                            s.DisplayName,
                            s.SkillType,
                            s.TargetingType,
                            NormalizeCombatEffectType(s.Key, s.EffectType),
                            s.ManaCost,
                            s.ChargeCost,
                            power,
                            s.Tags,
                            EffectFor(skillEffects, s.Key),
                            category: s.Category,
                            basePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality,
                            tacticalRange: s.TacticalRange,
                            tacticalAreaShape: ParseTacticalAreaShape(s.TacticalAreaShape),
                            requiresLineOfSight: s.RequiresLineOfSight,
                            cooldown: s.Cooldown,
                            isUltimate: s.IsUltimate,
                            emotionalRegister: s.EmotionalRegister);
                    })
                    .ToArray();

                var scaledEnemyVitality = Math.Max(1, scaled.Vitality);

                return new[]
                {
                    Combatant.CreateEnemy(
                        sourceKey: enemy.EnemyKey,
                        displayName: enemy.DisplayName,
                        archetype: enemy.Archetype,
                        maxVitality: scaledEnemyVitality,
                        skills: skills,
                        startingGuard: enemyStartingGuard,
                        attackPower: scaledAttackPower,
                        defense: scaledDefense,
                        speed: Math.Max(1, scaledSpeed),
                        focus: scaledFocus,
                        magicAttack: scaledMagicAttack,
                        magicDefense: scaledMagicDefense,
                        movement: enemy.Movement,
                        naturalEmotionalType: EmotionalTypeCode.ParseRequired(
                            enemy.EmotionalRegister,
                            $"Enemy '{enemy.EnemyKey}' natural emotional register"),
                        // Base Mana = 85% of (scaled) Vitality — enemies "cast freely" (see
                        // CombatSkillEffectResolver.ConsumeResources), so this is purely
                        // informational, but stays consistent with the player-side rule
                        // and scales automatically with depth/difficulty instead of
                        // relying on ~40 hand-authored catalog values.
                        mana: (int)Math.Round(scaledEnemyVitality * EnemyManaToVitalityRatio, MidpointRounding.AwayFromZero))
                };
            })
            .ToArray();

        if (activeModifiers.Any(m => m.Type == RunModifierType.TurnOrderReverse && !m.IsConsumed))
        {
            ApplyTurnOrderReversal(allies.Concat(enemies));
        }

        if (activeModifiers.Any(m => m.Type == RunModifierType.TurnOrderLock && !m.IsConsumed))
        {
            ApplyStrictInitiativeOrder(allies.Concat(enemies));
        }

        if (activeModifiers.Any(m => m.Type == RunModifierType.CruelDestinyForEveryone && !m.IsConsumed))
        {
            ApplyCruelDestinyToEveryone(allies.Concat(enemies));
        }

        ApplyClimateStatBundle(activeClimate, allies.Concat(enemies));
        ApplyAllyHealingBonus(activeModifiers, allies);
        ApplySilenceDuBundle(activeModifiers, allies.Concat(enemies));

        var hitCounterDoubleDamageEnabled = activeModifiers
            .Any(m => m.Type == RunModifierType.HitCounterDoubleDamage && !m.IsConsumed);
        var firstHitCriticalEnabled = activeModifiers
            .Any(m => m.Type == RunModifierType.FirstHitCritical && !m.IsConsumed);
        var lowHpDamageAmplificationEnabled = activeModifiers
            .Any(m => m.Type == RunModifierType.DamageAmplificationBelowHpThreshold && !m.IsConsumed);

        return new CombatRoster(
            allies,
            enemies,
            hitCounterDoubleDamageEnabled,
            firstHitCriticalEnabled,
            lowHpDamageAmplificationEnabled,
            ComputeDotDurationExtensionTicks(activeModifiers),
            ComputeDuelDamageAsymmetryEnabled(activeModifiers),
            dotMagnitudeBonus,
            healingBlocked,
            falaiseWindEnabled,
            postDeathBasicAttackOnlyEnabled,
            tapisPropreEnabled,
            thirdCupHealCorruptionEnabled,
            presentationsEnabled,
            miroirEnabled,
            forgottenSkillKey);
    }

    /// <summary>
    /// "Loi du Sablier Renversé" (law.sablier, Durée: Salle) — "les combattants les plus
    /// lents agissent en premier, les plus rapides en dernier."
    /// Mirrors every combatant's base Speed around the roster's [min, max] range via a
    /// flat Speed StatModifier, producing a genuinely reversed initiative order. Re-derived
    /// fresh for every combat in the room for as long as the law's RunModifier
    /// (UntilRoomEnds) stays unconsumed — same pattern as RoomClimate/AttackTypeOverride.
    /// </summary>
    private static void ApplyTurnOrderReversal(IEnumerable<Combatant> combatants)
    {
        var roster = combatants.ToArray();
        if (roster.Length == 0)
            return;

        var minSpeed = roster.Min(c => c.BaseStatSnapshot.Speed);
        var maxSpeed = roster.Max(c => c.BaseStatSnapshot.Speed);

        foreach (var combatant in roster)
        {
            var invertedSpeed = minSpeed + maxSpeed - combatant.BaseStatSnapshot.Speed;
            var delta = invertedSpeed - combatant.BaseStatSnapshot.Speed;

            if (delta == 0)
                continue;

            combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                key: "law-sablier-renverse:speed",
                displayName: "Sablier renversé",
                kind: StatusEffectKind.StatModifier,
                currentTick: 0,
                durationTicks: 0,
                magnitude: delta,
                stat: CombatStat.Speed,
                isPermanent: true));
        }
    }

    /// <summary>
    /// "Loi de la File Indienne" (law.file-indienne, Durée: Salle) — "aucun combattant
    /// ne peut agir une seconde fois tant que tous les combattants n'ont pas agi une
    /// fois." Every combatant's Speed is flattened to the roster's average via a flat
    /// StatModifier, so Initiative becomes the stable tie-break.
    /// </summary>
    private static void ApplyStrictInitiativeOrder(IEnumerable<Combatant> combatants)
    {
        var roster = combatants.ToArray();
        if (roster.Length == 0)
            return;

        var averageSpeed = (int)Math.Round(roster.Average(c => c.BaseStatSnapshot.Speed));

        foreach (var combatant in roster)
        {
            var delta = averageSpeed - combatant.BaseStatSnapshot.Speed;

            if (delta == 0)
                continue;

            combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                key: "law-file-indienne:speed",
                displayName: "File indienne",
                kind: StatusEffectKind.StatModifier,
                currentTick: 0,
                durationTicks: 0,
                magnitude: delta,
                stat: CombatStat.Speed,
                isPermanent: true));
        }
    }

    /// <summary>"Loi de la Destinée" (law.destinee, Durée: Salle) — "TOUS les combattants
    /// (équipe et ennemis) reçoivent « Une destinée cruelle »" for the room. Reuses the
    /// exact same permanent effect bundle as the existing legendary player skill
    /// canon.skill.destinee-cruelle (+20% Attack/Defense/Speed/Focus, -15% additional Speed,
    /// 10%-max-HP DoT with no end) per the compendium's own note ("application
    /// universelle du seul sort canon permanent"), applied to every combatant instead
    /// of just the caster.</summary>
    private static void ApplyCruelDestinyToEveryone(IEnumerable<Combatant> combatants)
    {
        foreach (var combatant in combatants)
        {
            foreach (var stat in new[] { CombatStat.AttackPower, CombatStat.Defense, CombatStat.Speed, CombatStat.Focus })
            {
                combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                    key: $"law-destinee:{stat}",
                    displayName: "Une destinée cruelle",
                    kind: StatusEffectKind.StatModifier,
                    currentTick: 0,
                    durationTicks: 0,
                    magnitude: 20,
                    stat: stat,
                    isMagnitudePercentOfBaseStat: true,
                    isPermanent: true));
            }

            combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                key: "law-destinee:tempo",
                displayName: "Une destinée cruelle",
                kind: StatusEffectKind.StatModifier,
                currentTick: 0,
                durationTicks: 0,
                magnitude: -15,
                stat: CombatStat.Speed,
                isPermanent: true));

            combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                key: "law-destinee:dot",
                displayName: "Une destinée cruelle",
                kind: StatusEffectKind.DamageOverTime,
                currentTick: 0,
                durationTicks: 0,
                magnitude: 10,
                tickInterval: CombatTime.TicksPerTurn,
                isMagnitudePercentOfMax: true,
                isPermanent: true));
        }
    }

    /// <summary>StatKind strings authored on conditional equipment effects (e.g. "AttackPower",
    /// "MagicAttack") that have a matching CombatStat "base stat" usable with
    /// isMagnitudePercentOfBaseStat. "MaxVitality"/"Mana" are intentionally absent — CombatStat
    /// has no equivalent virtual stat for them (documented simplification: an "all-stats"
    /// conditional bonus like Ombrelle du jardinier's only reaches 6 of its 8 named stats).</summary>
    private static readonly IReadOnlyDictionary<string, CombatStat> ConditionalEquipmentStatKinds =
        new Dictionary<string, CombatStat>(StringComparer.OrdinalIgnoreCase)
        {
            ["AttackPower"] = CombatStat.AttackPower,
            ["Defense"] = CombatStat.Defense,
            ["Speed"] = CombatStat.Speed,
            ["Focus"] = CombatStat.Focus,
            ["MagicAttack"] = CombatStat.MagicAttack,
            ["MagicDefense"] = CombatStat.MagicDefense,
        };

    /// <summary>
    /// Room/weather-conditional equipment StatBonus/StatBonusPercent effects (e.g. Boussole
    /// du Pèlerin's "+10% Vitesse dans la Montagne", Couronne de sel's weather-gated magic
    /// attack) — excluded from PlayerStatMerger's static run-start bake (see its Condition
    /// filter) and re-evaluated fresh here against the room actually entered and the climate
    /// actually rolled for this combat, instead of being baked into a static run-start stat.
    /// </summary>
    private static void ApplyConditionalEquipmentStatBundle(
        Combatant? protagonist,
        string? roomTheme,
        RoomClimate? climate,
        IReadOnlyCollection<CatalogItemEquipmentEffect> conditionalEquipmentEffects)
    {
        if (protagonist is null || conditionalEquipmentEffects.Count == 0)
            return;

        foreach (var effect in conditionalEquipmentEffects)
        {
            if (effect.Condition is not { } condition
                || effect.StatKind is not { } statKind
                || effect.Amount is not { } amount
                || !ConditionalEquipmentStatKinds.TryGetValue(statKind, out var stat))
                continue;

            var isPercent = string.Equals(effect.Kind, "StatBonusPercent", StringComparison.OrdinalIgnoreCase);
            if (!isPercent && !string.Equals(effect.Kind, "StatBonus", StringComparison.OrdinalIgnoreCase))
                continue;

            var matches = condition.StartsWith("room:", StringComparison.OrdinalIgnoreCase)
                ? string.Equals(condition["room:".Length..], roomTheme, StringComparison.OrdinalIgnoreCase)
                : condition.StartsWith("weather:", StringComparison.OrdinalIgnoreCase)
                    ? climate is not null
                        && string.Equals(condition["weather:".Length..], climate.Value.ToString(), StringComparison.OrdinalIgnoreCase)
                    : false;
            if (!matches)
                continue;

            protagonist.ApplyStatusEffect(CombatStatusEffect.Create(
                key: $"conditional-equip:{condition}:{statKind}",
                displayName: "Effet d'équipement conditionnel",
                kind: StatusEffectKind.StatModifier,
                currentTick: 0,
                durationTicks: 0,
                magnitude: amount,
                stat: stat,
                isMagnitudePercentOfBaseStat: isPercent,
                isPermanent: true));
        }
    }

    /// <summary>
    /// Chapitre II — the three climates whose effect is a flat stat bundle applied to
    /// EVERY combatant for the room (player and enemy alike, same convention as
    /// ApplyCruelDestinyToEveryone): "Brume" (law.voile, -3 Focus flat), "Orage"
    /// (law.accords, +15% magic damage), "Pluie de cendres" (law.deuil-sec, -25%
    /// healing received/applied, +15% DoT damage — "dégâts de feu" reinterpreted as a
    /// DoT bonus since no elemental "fire" type exists, documented simplification).
    /// "Pluie violacée" (Marée Haute) is handled separately via Combat.DotMagnitudeBonus
    /// since it needs to be threaded through the resolver, not just a StatModifier.
    /// </summary>
    private static void ApplyClimateStatBundle(RoomClimate? climate, IEnumerable<Combatant> combatants)
    {
        if (climate is not (
            RoomClimate.Brume
            or RoomClimate.Orage
            or RoomClimate.PluieDeCendres
            or RoomClimate.PluieViolacee))
            return;

        foreach (var combatant in combatants)
        {
            switch (climate)
            {
                case RoomClimate.Brume:
                    combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                        key: "climat-brume:focus",
                        displayName: "Brume",
                        kind: StatusEffectKind.StatModifier,
                        currentTick: 0,
                        durationTicks: 0,
                        magnitude: -25,
                        stat: CombatStat.Focus,
                        isMagnitudePercentOfBaseStat: true,
                        isPermanent: true));
                    break;

                case RoomClimate.Orage:
                    combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                        key: "climat-orage:magic-damage",
                        displayName: "Orage",
                        kind: StatusEffectKind.StatModifier,
                        currentTick: 0,
                        durationTicks: 0,
                        magnitude: 15,
                        stat: CombatStat.MagicDamageBonus,
                        isPermanent: true));
                    break;

                case RoomClimate.PluieDeCendres:
                    combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                        key: "climat-pluie-de-cendres:healing",
                        displayName: "Pluie de cendres",
                        kind: StatusEffectKind.StatModifier,
                        currentTick: 0,
                        durationTicks: 0,
                        magnitude: -25,
                        stat: CombatStat.HealingBonus,
                        isPermanent: true));
                    combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                        key: "climat-pluie-de-cendres:dot",
                        displayName: "Pluie de cendres",
                        kind: StatusEffectKind.StatModifier,
                        currentTick: 0,
                        durationTicks: 0,
                        magnitude: 25,
                        stat: CombatStat.FireDamageBonus,
                        isPermanent: true));
                    break;

                case RoomClimate.PluieViolacee:
                    combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                        key: "climat-pluie-violacee:periodic-damage",
                        displayName: "Pluie violacée",
                        kind: StatusEffectKind.StatModifier,
                        currentTick: 0,
                        durationTicks: 0,
                        magnitude: 25,
                        stat: CombatStat.DotDamageBonus,
                        isPermanent: true));
                    break;
            }
        }
    }

    /// <summary>"Édit du Souvenir Doux" (law.souvenir-doux): "tous les soins reçus par
    /// l'équipe sont majorés de +20%" — allies ONLY (unlike the climate bundles above,
    /// which are symmetric). Value is the flat percentage bonus as authored in the
    /// catalog.</summary>
    private static void ApplyAllyHealingBonus(IReadOnlyCollection<RunModifier> activeModifiers, IEnumerable<Combatant> allies)
    {
        var bonusPercent = activeModifiers
            .Where(m => m.Type == RunModifierType.AllyHealingBonus && !m.IsConsumed)
            .Sum(m => m.Value);

        if (bonusPercent == 0)
            return;

        foreach (var ally in allies)
        {
            ally.ApplyStatusEffect(CombatStatusEffect.Create(
                key: "law-souvenir-doux:healing",
                displayName: "Souvenir doux",
                kind: StatusEffectKind.StatModifier,
                currentTick: 0,
                durationTicks: 0,
                magnitude: (int)Math.Round(bonusPercent),
                stat: CombatStat.HealingBonus,
                isPermanent: true));
        }
    }

    /// <summary>"Loi du Silence Dû" (law.silence-du): "les sorts coûtent +2 mana pour tous
    /// les combattants ; les attaques physiques infligent +8% de dégâts" — symmetric,
    /// both sides. The mana-cost half only has a real effect on the Player side in
    /// practice (CombatSkillEffectResolver.ConsumeResources: "enemies cast freely"), a
    /// pre-existing engine simplification unrelated to this law.</summary>
    private static void ApplySilenceDuBundle(IReadOnlyCollection<RunModifier> activeModifiers, IEnumerable<Combatant> combatants)
    {
        if (!activeModifiers.Any(m => m.Type == RunModifierType.SilenceDuActive && !m.IsConsumed))
            return;

        foreach (var combatant in combatants)
        {
            combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                key: "law-silence-du:physical-damage",
                displayName: "Silence dû",
                kind: StatusEffectKind.StatModifier,
                currentTick: 0,
                durationTicks: 0,
                magnitude: 8,
                stat: CombatStat.PhysicalDamageBonus,
                isPermanent: true));
            combatant.ApplyStatusEffect(CombatStatusEffect.Create(
                key: "law-silence-du:mana-cost",
                displayName: "Silence dû",
                kind: StatusEffectKind.StatModifier,
                currentTick: 0,
                durationTicks: 0,
                magnitude: 2,
                stat: CombatStat.FlatManaCostBonus,
                isPermanent: true));
        }
    }

    /// <summary>"Loi de l'Écriture" (law.ecriture): "tous les DoT durent +2 tours" — the
    /// RunModifier's Value is a bonus TURN count (as authored in the catalog), converted
    /// here to ticks so Combat only ever deals in its native tick unit.</summary>
    private static int ComputeDotDurationExtensionTicks(IReadOnlyCollection<RunModifier> activeModifiers)
    {
        var bonusTurns = activeModifiers
            .Where(m => m.Type == RunModifierType.DotDurationExtension && !m.IsConsumed)
            .Sum(m => m.Value);

        return (int)Math.Round(bonusTurns * CombatTime.TicksPerTurn);
    }

    private static bool ComputeDuelDamageAsymmetryEnabled(IReadOnlyCollection<RunModifier> activeModifiers)
        => activeModifiers.Any(m => m.Type == RunModifierType.DuelDamageAsymmetry && !m.IsConsumed);

    // Looks up the durable statuses a skill applies, by skill key (catalog-sourced).
    private static IReadOnlyList<SkillStatusEffectSpec>? EffectFor(
        IReadOnlyDictionary<string, IReadOnlyList<SkillStatusEffectSpec>>? effects,
        string key)
        => effects is not null && effects.TryGetValue(key, out var specs) ? specs : null;

    private static EmotionalType? ResolveAttackTypeOverride(IReadOnlyCollection<RunModifier> runModifiers)
    {
        var modifier = runModifiers
            .Where(m => m.Type == RunModifierType.AttackTypeOverride && !m.IsConsumed)
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefault();

        if (modifier is null)
        {
            return null;
        }

        var value = (int)Math.Round(modifier.Value);

        return Enum.IsDefined(typeof(EmotionalType), value) && value != (int)EmotionalType.Neutral
            ? (EmotionalType)value
            : null;
    }

    private static RoomClimate? ResolveActiveClimate(
        Guid roomId,
        IReadOnlyCollection<RunModifier> runModifiers)
    {
        var modifier = runModifiers
            .Where(modifier =>
                modifier.Type == RunModifierType.RoomClimate &&
                !modifier.IsConsumed &&
                modifier.ExpiresAtRoomId == roomId)
            .OrderByDescending(modifier => modifier.CreatedAtUtc)
            .FirstOrDefault();

        return modifier?.Value switch
        {
            1 => RoomClimate.Grey,
            2 => RoomClimate.Rain,
            3 => RoomClimate.Heatwave,
            4 => RoomClimate.Hail,
            // Chapitre II "Lois climatiques" — the Compendium's actual 5 canonical
            // climate states (Accalmie/Pluie violacée/Brume/Orage/Pluie de cendres) do
            // not match the 4 legacy values above (built before the Compendium existed,
            // never seeded by any catalog content — kept only for the existing test
            // suite). Added on, not renumbered, per the append-only convention. Accalmie
            // (law.repit) is not mapped: it needs a Sévère-law-suspension mechanic
            // (RunModifierType.SuspendSevereLaws) that doesn't exist yet.
            5 => RoomClimate.Brume,
            6 => RoomClimate.Orage,
            7 => RoomClimate.PluieDeCendres,
            8 => RoomClimate.PluieViolacee,
            9 => RoomClimate.Accalmie,
            _ => null
        };
    }

    private enum RoomClimate
    {
        Grey,
        Rain,
        Heatwave,
        Hail,
        Brume,
        Orage,
        PluieDeCendres,
        PluieViolacee,
        Accalmie
    }

    private static string NormalizeCombatEffectType(string skillKey, string effectType)
    {
        if (string.Equals(skillKey, "skill.basic.guard", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(effectType, "AddCurrentGuard", StringComparison.OrdinalIgnoreCase))
        {
            return "Guard";
        }

        return effectType;
    }

    /// <summary>
    /// Mina's legendary "Protection de Him'Lit" (owned — not equipped): a permanent,
    /// innate buff/debuff bundle applied to the protagonist at the start of every combat,
    /// narratively "a ward that wasn't quite meant for you" — +10 guard, +5% attack power,
    /// +5% speed, -10% additional speed, -5% skill mana/charge cost.
    /// </summary>
    private static void ApplyHimLitProtection(Combatant protagonist)
    {
        const string DisplayName = "Ce n'était pas pour vous...";

        protagonist.GainGuard(10);

        protagonist.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "himlit-protection:attack", displayName: DisplayName,
            kind: StatusEffectKind.StatModifier, currentTick: 0, durationTicks: 0,
            magnitude: 5, stat: CombatStat.AttackPower,
            isMagnitudePercentOfBaseStat: true, isPermanent: true));

        protagonist.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "himlit-protection:speed", displayName: DisplayName,
            kind: StatusEffectKind.StatModifier, currentTick: 0, durationTicks: 0,
            magnitude: 5, stat: CombatStat.Speed,
            isMagnitudePercentOfBaseStat: true, isPermanent: true));

        protagonist.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "himlit-protection:tempo", displayName: DisplayName,
            kind: StatusEffectKind.StatModifier, currentTick: 0, durationTicks: 0,
            magnitude: -10, stat: CombatStat.Speed,
            isMagnitudePercentOfBaseStat: true, isPermanent: true));

        protagonist.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "himlit-protection:cost", displayName: DisplayName,
            kind: StatusEffectKind.StatModifier, currentTick: 0, durationTicks: 0,
            magnitude: -5, stat: CombatStat.SkillCostReductionPercent,
            isPermanent: true));
    }

    private static int ApplySpeedMultiplier(int baseSpeed, double speedMultiplier)
    {
        return speedMultiplier == 1.0 ? baseSpeed : Math.Max(1, (int)Math.Round(baseSpeed * speedMultiplier));
    }

    private static int ScalePlayerSkillPower(string effectType, int basePower, double attackPowerMultiplier)
    {
        if (!string.Equals(effectType, "Damage", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(effectType, "DamageVitality", StringComparison.OrdinalIgnoreCase))
        {
            return basePower;
        }

        return Math.Max(1, (int)Math.Round(basePower * attackPowerMultiplier));
    }

    private static int ScaleEnemySkillPower(
        string effectType,
        int basePower,
        double climateMultiplier,
        PalaceRoomState palaceRoomState)
    {
        var multiplier = climateMultiplier;
        if (palaceRoomState == PalaceRoomState.Painful && IsDamageEffect(effectType))
        {
            multiplier *= 0.90;
        }

        return Math.Max(1, (int)Math.Round(basePower * multiplier));
    }

    private static bool IsDamageEffect(string effectType)
    {
        return string.Equals(effectType, "Damage", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(effectType, "DamageVitality", StringComparison.OrdinalIgnoreCase);
    }

    private static TacticalAreaShape ParseTacticalAreaShape(string value)
    {
        if (Enum.TryParse<TacticalAreaShape>(value, ignoreCase: true, out var shape))
            return shape;

        throw new DomainException(
            $"Unsupported tactical area shape '{value}'. Expected Single, Cross, Diamond or Map.");
    }

    private static IReadOnlyCollection<CombatantSkill> GetDefaultAllySkills(
        double attackPowerMultiplier,
        IReadOnlyDictionary<string, IReadOnlyList<SkillStatusEffectSpec>>? skillEffects)
    {
        return
        [
            CombatantSkill.Create(
                key: "skill.basic.strike",
                displayName: "Frappe",
                skillType: "Damage",
                targetingType: "SingleEnemy",
                effectType: "Damage",
                manaCost: 0,
                chargeCost: 0,
                basePower: ScalePlayerSkillPower("Damage", 10, attackPowerMultiplier),
                statusEffects: EffectFor(skillEffects, "skill.basic.strike")),
            CombatantSkill.Create(
                key: "skill.basic.guard",
                displayName: "Garde",
                skillType: "Defense",
                targetingType: "Self",
                effectType: "Guard",
                manaCost: 0,
                chargeCost: 0,
                basePower: 5,
                statusEffects: EffectFor(skillEffects, "skill.basic.guard"))
        ];
    }
}
