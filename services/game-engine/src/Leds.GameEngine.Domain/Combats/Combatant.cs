using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats;

public sealed class Combatant
{
    private Combatant(
        CombatantId id,
        string sourceKey,
        string displayName,
        CombatantSide side,
        string archetype,
        EmotionalType naturalEmotionalType,
        int maxVitality,
        int currentVitality,
        int guard,
        int baseGuard,
        int mana,
        int maxMana,
        decimal charge,
        CombatantStatus status,
        IReadOnlyCollection<CombatantSkill> skills,
        CombatantBaseStatSnapshot baseStatSnapshot,
        CombatantRuntimeState runtimeState,
        bool hasActedThisCombat = false,
        Guid? characterInstanceId = null)
    {
        Id = id;
        SourceKey = sourceKey;
        DisplayName = displayName;
        Side = side;
        Archetype = archetype;
        NaturalEmotionalType = naturalEmotionalType;
        MaxVitality = maxVitality;
        CurrentVitality = currentVitality;
        Guard = guard;
        BaseGuard = baseGuard;
        Mana = mana;
        MaxMana = maxMana;
        HasActedThisCombat = hasActedThisCombat;
        Charge = charge;
        Status = status;
        _permanentSkills = skills;
        BaseStatSnapshot = baseStatSnapshot;
        RuntimeState = runtimeState;
        CharacterInstanceId = characterInstanceId;
    }

    public CombatantId Id { get; }
    public string SourceKey { get; }
    public string SourceDefinitionKey => SourceKey;
    public Guid? CharacterInstanceId { get; }
    public string DisplayName { get; }
    public CombatantSide Side { get; }
    public string Archetype { get; }
    public EmotionalType NaturalEmotionalType { get; }
    public int MaxVitality { get; }
    public int CurrentVitality { get; private set; }
    public int Guard { get; private set; }

    /// <summary>
    /// The passive guard floor restored at the start of each new round.
    /// Set from StartingGuardBonus run modifiers at combat creation.
    /// </summary>
    public int BaseGuard { get; private set; }

    public int Mana { get; private set; }
    public int MaxMana { get; }
    public decimal Charge { get; private set; }
    public CombatantStatus Status { get; private set; }

    /// <summary>
    /// "Loi du Tapis Propre" (law.tapis-propre): true once this combatant has taken
    /// any action (of any kind) in the current combat. A first turn spent on movement,
    /// buff or heal still counts as "having acted" —
    /// only a Damage-type skill is gated while this is false (see
    /// CombatSkillActionValidator).
    /// </summary>
    public bool HasActedThisCombat { get; private set; }

    public void MarkActedThisCombat() => HasActedThisCombat = true;

    private readonly IReadOnlyCollection<CombatantSkill> _permanentSkills;

    /// <summary>The combatant's own, permanently-owned skills (what persistence should
    /// save) — excludes anything temporarily granted via a SkillGrant status effect
    /// (e.g. "Création"). Use <see cref="Skills"/> for anything gameplay-facing.</summary>
    public IReadOnlyCollection<CombatantSkill> PermanentSkills => _permanentSkills;

    /// <summary>All skills currently usable by this combatant: its own permanent kit
    /// plus any not-yet-expired skills temporarily granted by a SkillGrant status
    /// effect (e.g. "Création", the Architect's legendary skill). This is what action
    /// validation, the runtime DTO, and enemy AI planning all read.</summary>
    public IReadOnlyCollection<CombatantSkill> Skills => _statusEffects.Count == 0
        ? _permanentSkills
        : _permanentSkills
            .Concat(_statusEffects
                .Where(e => e.Kind == StatusEffectKind.SkillGrant)
                .SelectMany(e => e.GrantedSkills))
            .ToArray();

    public bool IsDefeated => Status == CombatantStatus.Defeated;
    /// <summary>
    /// Optional emotional attack type override, set from an AttackTypeOverride run
    /// modifier at combat creation (e.g. an item that changes the hero's type).
    /// When null, the combatant's attack type comes from its default type profile.
    /// </summary>
    public EmotionalType? AttackTypeOverride { get; private set; }

    /// <summary>
    /// Sets (or clears) the emotional attack type override. Applied at combat
    /// creation and restored on rehydration; never mutated mid-turn.
    /// </summary>
    public void ApplyAttackTypeOverride(EmotionalType? attackType)
    {
        AttackTypeOverride = attackType;
    }

    /// <summary>
    /// Percentage (0-100) reduction to damage of a given incoming <see cref="EmotionalType"/>,
    /// granted by equipped items (e.g. Craie créatrice: -15% Mémoire). Independent of the
    /// categorical weak/resist/immune type system. Empty = no reduction.
    /// </summary>
    public IReadOnlyDictionary<EmotionalType, int> TypedDamageReductionPercent { get; private set; }
        = new Dictionary<EmotionalType, int>();

    /// <summary>
    /// Sets (or clears) the equipment-driven typed damage reductions. Applied at combat
    /// creation and restored on rehydration; never mutated mid-turn.
    /// </summary>
    public void ApplyTypedDamageReductions(IReadOnlyDictionary<EmotionalType, int>? reductions)
    {
        TypedDamageReductionPercent = reductions ?? new Dictionary<EmotionalType, int>();
    }

    /// <summary>
    /// Percentage points added to this combatant's hit chance (e.g. Lunettes
    /// d'érudit: +10%), granted by equipped items. Stacks additively with the
    /// base hit chance in <see cref="Typing.HitChanceCalibration"/>.
    /// </summary>
    public int HitChanceBonusPercent { get; private set; }

    /// <summary>
    /// Percentage (0-100) by which incoming DamageOverTime effects have their
    /// duration/per-tick damage reduced, granted by equipped items
    /// (e.g. Main de Khasma: -25% durée, -15% dégâts).
    /// </summary>
    public int DotDurationReductionPercent { get; private set; }
    public int DotDamageReductionPercent { get; private set; }

    /// <summary>
    /// Percentage points added to the DAMAGE DEALT by DamageOverTime effects this
    /// combatant applies to others, granted by equipped items (e.g. l'Écrivain's
    /// Plume d'écrivain: +5%). Distinct from <see cref="DotDamageReductionPercent"/>,
    /// which reduces incoming DoT damage taken. Permanent for the run.
    /// </summary>
    public int DotDamageBonusPercent { get; private set; }

    /// <summary>
    /// Percentage points added to / subtracted from Magic-category skill damage,
    /// granted by equipped items (e.g. Pomenian's monocle: +10% offensive spell
    /// damage). Permanent for the run — distinct from the temporary, skill-driven
    /// component summed in <see cref="EffectiveMagicDamageBonusPercent"/>.
    /// </summary>
    public int MagicDamageBonusPercent { get; private set; }
    public int MagicDamageReductionPercent { get; private set; }

    /// <summary>
    /// Percentage points added directly to critical hit chance, granted by equipped
    /// items (e.g. Iris's Doudou de Ethan: +5%), on top of the chance derived from
    /// Focus. Permanent for the run.
    /// </summary>
    public int CriticalChanceBonusPercent { get; private set; }

    /// <summary>
    /// Percentage points added to ALL healing this combatant applies (skills and items),
    /// granted by equipped items (e.g. Majordome's legendary "La tasse du majordome":
    /// +15%). Permanent for the run — distinct from the temporary, skill-driven component
    /// summed in <see cref="EffectiveHealingBonusPercent"/>.
    /// </summary>
    public int HealingBonusPercent { get; private set; }

    /// <summary>
    /// Sets (or clears) the equipment-driven hit chance bonus, DOT reductions/bonus, Magic
    /// damage bonus/reduction, critical chance bonus, and healing bonus. Applied at combat
    /// creation and restored on rehydration; never mutated mid-turn.
    /// </summary>
    public void ApplyEquipmentCombatModifiers(
        int hitChanceBonusPercent, int dotDurationReductionPercent, int dotDamageReductionPercent,
        int magicDamageBonusPercent = 0, int magicDamageReductionPercent = 0, int criticalChanceBonusPercent = 0,
        int dotDamageBonusPercent = 0, int healingBonusPercent = 0)
    {
        HitChanceBonusPercent = hitChanceBonusPercent;
        DotDurationReductionPercent = dotDurationReductionPercent;
        DotDamageReductionPercent = dotDamageReductionPercent;
        DotDamageBonusPercent = dotDamageBonusPercent;
        MagicDamageBonusPercent = magicDamageBonusPercent;
        MagicDamageReductionPercent = magicDamageReductionPercent;
        CriticalChanceBonusPercent = criticalChanceBonusPercent;
        HealingBonusPercent = healingBonusPercent;
    }

    /// <summary>
    /// Total Magic-category damage bonus (%): permanent equipment component plus any
    /// active temporary StatModifier(MagicDamageBonus) status effect (e.g. Pomenian's
    /// "Connaissance académique").
    /// </summary>
    public int EffectiveMagicDamageBonusPercent
        => MagicDamageBonusPercent + EffectiveStat(CombatStat.MagicDamageBonus, 0);

    /// <summary>
    /// Total healing bonus (%): permanent equipment component (e.g. La tasse du
    /// majordome) plus any active temporary StatModifier(HealingBonus) status effect.
    /// Read by <see cref="Leds.GameEngine.Application.Combats.Effects.CombatSkillEffectResolver"/>
    /// when this combatant applies healing to a target.
    /// </summary>
    public int EffectiveHealingBonusPercent
        => HealingBonusPercent + EffectiveStat(CombatStat.HealingBonus, 0);
    public int EffectiveFireDamageBonusPercent
        => EffectiveStat(CombatStat.FireDamageBonus, 0);

    /// <summary>
    /// Total DoT-damage-dealt bonus (%): permanent equipment component (e.g. Plume
    /// d'écrivain) plus any active temporary StatModifier(DotDamageBonus) status
    /// effect. Read by <see cref="Leds.GameEngine.Application.Combats.Effects.CombatSkillEffectResolver"/>
    /// when this combatant applies a new DamageOverTime effect to a target.
    /// </summary>
    public int EffectiveDotDamageBonusPercent
        => DotDamageBonusPercent + EffectiveStat(CombatStat.DotDamageBonus, 0);

    /// <summary>
    /// Total Magic-category incoming damage reduction (%): permanent equipment
    /// component plus any active temporary StatModifier(MagicDamageReduction) status
    /// effect.
    /// </summary>
    public int EffectiveMagicDamageReductionPercent
        => MagicDamageReductionPercent + EffectiveStat(CombatStat.MagicDamageReduction, 0);

    /// <summary>
    /// Total Physical-category damage bonus (%) — symmetric counterpart to
    /// <see cref="EffectiveMagicDamageBonusPercent"/>, virtual only (no permanent
    /// equipment component exists yet). "Loi du Silence Dû" (law.silence-du) is its
    /// only source today.
    /// </summary>
    public int EffectivePhysicalDamageBonusPercent
        => EffectiveStat(CombatStat.PhysicalDamageBonus, 0);

    /// <summary>
    /// Flat (not percentage) mana cost added to this combatant's next skill casts, on
    /// top of <see cref="EffectiveSkillCostReductionPercent"/>'s percentage reduction.
    /// Read by <see cref="Leds.GameEngine.Application.Combats.Effects.CombatSkillEffectResolver"/>.ConsumeResources.
    /// "Loi du Silence Dû" (law.silence-du) is its only source today.
    /// </summary>
    public int EffectiveFlatManaCostBonus
        => EffectiveStat(CombatStat.FlatManaCostBonus, 0);

    /// <summary>
    /// Total flat critical chance bonus (percentage points): permanent equipment
    /// component plus any active temporary StatModifier(CriticalChanceBonus) status
    /// effect. Added on top of the Focus-derived chance, still capped overall by
    /// CriticalHitCalibration.MaxCritChance.
    /// </summary>
    public int EffectiveCriticalChanceBonusPercent
        => CriticalChanceBonusPercent + EffectiveStat(CombatStat.CriticalChanceBonus, 0);

    /// <summary>
    /// Percentage points subtracted from a skill's mana/charge cost at cast time — e.g.
    /// Mina's "Protection de Him'Lit" (-5%, permanent). See CombatSkillEffectResolver.
    /// </summary>
    public int EffectiveSkillCostReductionPercent => EffectiveStat(CombatStat.SkillCostReductionPercent, 0);

    // ── Threat (enemy targeting) ────────────────────────────────────────────

    /// <summary>Accumulated threat this combatant has drawn over the fight so far.</summary>
    public double ThreatValue => RuntimeState.ThreatValue;

    /// <summary>Id of whoever most recently damaged this combatant, if any.</summary>
    public Guid? LastAttackerId => RuntimeState.LastAttackerId;

    public void AccrueThreat(double amount) => RuntimeState.AccrueThreat(amount);

    public void RecordLastAttacker(Guid attackerId) => RuntimeState.RecordLastAttacker(attackerId);

    /// <summary>Flags a "coup très puissant" (≥25% MaxVitality in one hit) since this
    /// combatant's last action — see <see cref="CombatantRuntimeState.RecordDamageTaken"/>.</summary>
    public void RecordDamageTaken(int amount) => RuntimeState.RecordDamageTaken(amount, MaxVitality);

    /// <summary>One-shot read of whether a "coup très puissant" landed since this
    /// combatant's last action; clears the flag once read.</summary>
    public bool ConsumePowerfulHitSinceLastAction() => RuntimeState.ConsumePowerfulHitSinceLastAction();

    /// <summary>
    /// Immutable stat values frozen at combat creation.
    /// Canonical source for base/max/starting values.
    /// </summary>
    public CombatantBaseStatSnapshot BaseStatSnapshot { get; }

    /// <summary>
    /// Mutable runtime state during combat.
    /// Canonical source for current vitality, guard, mana, charge.
    /// </summary>
    public CombatantRuntimeState RuntimeState { get; }

    public static Combatant CreateAlly(
        string sourceKey,
        string displayName,
        string archetype,
        int maxVitality,
        int baseGuard = 0,
        IReadOnlyCollection<CombatantSkill>? skills = null,
        EmotionalType naturalEmotionalType = EmotionalType.Neutral,
        Guid? characterInstanceId = null)
    {
        var id = CombatantId.New();
        var snapshot = CombatantBaseStatSnapshot.Create(
            maxVitality: maxVitality,
            attackPower: 0,
            defense: 0,
            startingGuard: baseGuard,
            speed: 10,
            initiative: 0,
            focus: 0,
            mana: 0,
            charge: 0);

        var runtimeState = CombatantRuntimeState.Create(
            currentVitality: maxVitality,
            currentGuard: baseGuard);

        return new Combatant(
            id,
            sourceKey,
            displayName,
            CombatantSide.Player,
            archetype,
            naturalEmotionalType,
            maxVitality,
            currentVitality: maxVitality,
            guard: baseGuard,
            baseGuard: baseGuard,
            mana: 0,
            maxMana: runtimeState.MaxMana,
            charge: 0,
            CombatantStatus.Active,
            skills?.ToArray() ?? Array.Empty<CombatantSkill>(),
            snapshot,
            runtimeState,
            characterInstanceId: characterInstanceId);
    }

    public static Combatant CreateEnemy(
        string sourceKey,
        string displayName,
        string archetype,
        int maxVitality,
        IReadOnlyCollection<CombatantSkill>? skills = null,
        int startingGuard = 0,
        int attackPower = 0,
        int defense = 0,
        int speed = 10,
        int focus = 0,
        int magicAttack = 0,
        int magicDefense = 0,
        int mana = 0,
        int movement = 4,
        EmotionalType naturalEmotionalType = EmotionalType.Neutral,
        Guid? characterInstanceId = null)
    {
        var id = CombatantId.New();
        var snapshot = CombatantBaseStatSnapshot.Create(
            maxVitality: maxVitality,
            attackPower: attackPower,
            defense: defense,
            startingGuard: startingGuard,
            speed: speed,
            initiative: 0,
            focus: focus,
            mana: mana,
            charge: 0,
            magicAttack: magicAttack,
            magicDefense: magicDefense,
            movement: movement);

        var runtimeState = CombatantRuntimeState.Create(
            currentVitality: maxVitality,
            currentGuard: startingGuard,
            currentMana: mana,
            maxMana: mana);

        return new Combatant(
            id,
            sourceKey,
            displayName,
            CombatantSide.Enemy,
            archetype,
            naturalEmotionalType,
            maxVitality,
            currentVitality: maxVitality,
            guard: startingGuard,
            baseGuard: startingGuard,
            mana: mana,
            maxMana: runtimeState.MaxMana,
            charge: 0,
            CombatantStatus.Active,
            skills?.ToArray() ?? Array.Empty<CombatantSkill>(),
            snapshot,
            runtimeState,
            characterInstanceId: characterInstanceId);
    }

    public static Combatant Create(
        CombatantId id,
        string sourceKey,
        string displayName,
        CombatantSide side,
        string archetype,
        int maxVitality,
        int currentVitality,
        int guard,
        int baseGuard,
        int mana,
        decimal charge,
        IReadOnlyCollection<CombatantSkill>? skills = null,
        int attackPower = 0,
        int defense = 0,
        int speed = 10,
        int focus = 0,
        int? maxMana = null,
        int magicAttack = 0,
        int magicDefense = 0,
        int movement = 4,
        EmotionalType naturalEmotionalType = EmotionalType.Neutral,
        Guid? characterInstanceId = null)
    {
        if (id.Value == Guid.Empty)
            throw new DomainException("Combatant id is required.");

        if (string.IsNullOrWhiteSpace(sourceKey))
            throw new DomainException("Combatant source key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Combatant display name is required.");

        if (maxVitality <= 0)
            throw new DomainException("Combatant max vitality must be greater than zero.");

        if (currentVitality < 0 || currentVitality > maxVitality)
            throw new DomainException("Combatant current vitality must be between zero and max vitality.");

        if (guard < 0)
            throw new DomainException("Combatant guard must be non-negative.");

        if (baseGuard < 0)
            throw new DomainException("Combatant base guard must be non-negative.");

        if (mana < 0)
            throw new DomainException("Combatant mana must be non-negative.");

        if (charge < 0)
            throw new DomainException("Combatant charge must be non-negative.");

        var resolvedMaxMana = maxMana ?? int.MaxValue;

        if (mana > resolvedMaxMana)
            throw new DomainException("Combatant mana cannot exceed max mana.");

        var snapshot = CombatantBaseStatSnapshot.Create(
            maxVitality: maxVitality,
            attackPower: attackPower,
            defense: defense,
            startingGuard: baseGuard,
            speed: speed,
            initiative: 0,
            focus: focus,
            mana: mana,
            charge: (int)Math.Floor(charge),
            magicAttack: magicAttack,
            magicDefense: magicDefense,
            movement: movement);

        var runtimeState = CombatantRuntimeState.Create(
            currentVitality: currentVitality,
            currentGuard: guard,
            currentMana: mana,
            currentCharge: charge,
            maxMana: resolvedMaxMana);

        return new Combatant(
            id,
            sourceKey.Trim(),
            displayName.Trim(),
            side,
            archetype,
            naturalEmotionalType,
            maxVitality,
            currentVitality,
            guard,
            baseGuard,
            mana,
            resolvedMaxMana,
            charge,
            CombatantStatus.Active,
            skills?.ToArray() ?? Array.Empty<CombatantSkill>(),
            snapshot,
            runtimeState,
            characterInstanceId: characterInstanceId);
    }

    public void MarkDefeated()
    {
        if (Status == CombatantStatus.Defeated)
            throw new DomainException("Combatant is already defeated.");

        Status = CombatantStatus.Defeated;
        CurrentVitality = 0;
        RuntimeState.MarkDefeated();
    }

    public void Revive(int vitality)
    {
        if (!IsDefeated)
            throw new DomainException("Only a defeated combatant can be revived.");

        RuntimeState.Revive(MaxVitality, vitality);
        CurrentVitality = RuntimeState.CurrentVitality;
        Guard = RuntimeState.CurrentGuard;
        Status = CombatantStatus.Active;
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Damage amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatants cannot receive damage.");

        RuntimeState.ApplyDamage(amount);

        Guard = RuntimeState.CurrentGuard;
        CurrentVitality = RuntimeState.CurrentVitality;

        if (CurrentVitality == 0)
        {
            Status = CombatantStatus.Defeated;
        }
    }

    /// <summary>Direct vitality damage bypassing guard — used by DoT (poison/burn).</summary>
    public void ApplyVitalityDamage(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Damage amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatants cannot receive damage.");

        RuntimeState.ApplyVitalityDamage(amount);
        CurrentVitality = RuntimeState.CurrentVitality;

        if (CurrentVitality == 0)
        {
            Status = CombatantStatus.Defeated;
        }
    }

    public void GainGuard(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Guard amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatants cannot gain guard.");

        RuntimeState.GainGuard(amount);
        Guard = RuntimeState.CurrentGuard;
    }

    /// <summary>
    /// Restores Guard to BaseGuard at the start of each new round.
    /// Guard may still exceed BaseGuard from GainGuard (skill.basic.guard);
    /// we only restore if Guard has been consumed below BaseGuard.
    /// </summary>
    public void ResetGuardToBase()
    {
        if (IsDefeated) return;

        RuntimeState.ResetGuardToBase(BaseGuard);
        Guard = RuntimeState.CurrentGuard;
    }

    /// <summary>
    /// Rehydrates a combatant from a trusted persistence snapshot.
    /// This method must not be used to create a new gameplay combatant.
    /// </summary>
    public static Combatant Rehydrate(
        CombatantId id,
        string sourceKey,
        string displayName,
        CombatantSide side,
        string archetype,
        int maxVitality,
        int currentVitality,
        int guard,
        int baseGuard,
        int mana,
        decimal charge,
        CombatantStatus status,
        IReadOnlyCollection<CombatantSkill> skills,
        CombatantBaseStatSnapshot? baseStatSnapshot = null,
        CombatantRuntimeState? runtimeState = null,
        EmotionalType? attackTypeOverride = null,
        IReadOnlyDictionary<EmotionalType, int>? typedDamageReductionPercent = null,
        int hitChanceBonusPercent = 0,
        int dotDurationReductionPercent = 0,
        int dotDamageReductionPercent = 0,
        int magicDamageBonusPercent = 0,
        int magicDamageReductionPercent = 0,
        int criticalChanceBonusPercent = 0,
        int dotDamageBonusPercent = 0,
        int maxMana = int.MaxValue,
        int healingBonusPercent = 0,
        bool hasActedThisCombat = false,
        EmotionalType naturalEmotionalType = EmotionalType.Neutral,
        Guid? characterInstanceId = null)
    {
        var snapshot = baseStatSnapshot ?? CombatantBaseStatSnapshot.Rehydrate(
            Guid.NewGuid(),
            maxVitality,
            0,
            0,
            baseGuard,
            10,
            0,
            0,
            mana,
            (int)Math.Floor(charge),
            DateTime.UtcNow);

        var state = runtimeState ?? CombatantRuntimeState.Rehydrate(
            Guid.NewGuid(),
            currentVitality,
            guard,
            0,
            mana,
            charge,
            DateTime.UtcNow,
            maxMana: maxMana);

        var combatant = new Combatant(id, sourceKey, displayName, side, archetype, naturalEmotionalType, maxVitality, currentVitality, guard, baseGuard, mana, runtimeState?.MaxMana ?? maxMana, charge, status, skills, snapshot, state, hasActedThisCombat, characterInstanceId);
        combatant.AttackTypeOverride = attackTypeOverride;
        combatant.TypedDamageReductionPercent = typedDamageReductionPercent ?? new Dictionary<EmotionalType, int>();
        combatant.ApplyEquipmentCombatModifiers(
            hitChanceBonusPercent, dotDurationReductionPercent, dotDamageReductionPercent,
            magicDamageBonusPercent, magicDamageReductionPercent, criticalChanceBonusPercent,
            dotDamageBonusPercent, healingBonusPercent);
        return combatant;
    }

    public void ApplyHeal(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Heal amount must be greater than zero.");

        if (IsDefeated)
            throw new DomainException("Defeated combatant cannot be healed.");

        RuntimeState.ApplyHeal(MaxVitality, amount);
        CurrentVitality = RuntimeState.CurrentVitality;
    }

    public void DebugSetVitals(int vitality, int guard)
    {
        RuntimeState.DebugSetVitals(MaxVitality, vitality, guard);
        CurrentVitality = RuntimeState.CurrentVitality;
        Guard = RuntimeState.CurrentGuard;
        Status = CurrentVitality == 0
            ? CombatantStatus.Defeated
            : CombatantStatus.Active;
    }

    public void GainMana(int amount)
    {
        if (amount < 0)
            throw new DomainException("Mana gain amount cannot be negative.");

        RuntimeState.GainMana(amount);
        Mana = RuntimeState.CurrentMana;
    }

    public void GainCharge(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("Charge gain amount cannot be negative.");

        Charge = Math.Min(5m, Charge + Math.Round(amount, 1));
        RuntimeState.GainCharge(amount);
    }

    public void SpendMana(int amount)
    {
        if (amount <= 0)
            return;

        Mana = Math.Max(0, Mana - amount);
        RuntimeState.SpendMana(amount);
    }

    public void SpendCharge(int amount)
    {
        if (amount <= 0)
            return;

        Charge = Math.Max(0, Charge - amount);
        RuntimeState.SpendCharge(amount);
    }

    // ── Durable status effects (DoT/HoT, buffs/debuffs, control) ──────────────

    private readonly List<CombatStatusEffect> _statusEffects = new();

    /// <summary>Active durable effects (poison, regen, buffs/debuffs, stun…).</summary>
    public IReadOnlyCollection<CombatStatusEffect> StatusEffects => _statusEffects.AsReadOnly();

    public bool DispelOneStatusStack(Func<CombatStatusEffect, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var effect = _statusEffects.FirstOrDefault(predicate);
        if (effect is null)
            return false;

        if (effect.RemoveOneStack())
            _statusEffects.Remove(effect);

        return true;
    }

    /// <summary>
    /// Applies a status effect. Re-applying the same key adds stacks; remaining
    /// duration is NEVER refreshed by re-applying (see CombatStatusEffect.Reinforce) —
    /// only an explicit extend-duration mechanic changes it. A fresh key is added.
    /// No-op on a defeated combatant.
    /// </summary>
    public void ApplyStatusEffect(CombatStatusEffect effect)
    {
        if (effect is null)
            throw new DomainException("Status effect is required.");

        if (IsDefeated)
            return;

        var existing = _statusEffects.FirstOrDefault(
            e => string.Equals(e.Key, effect.Key, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            existing.Reinforce(effect);
        else
            _statusEffects.Add(effect);
    }

    /// <summary>Rehydration only: re-attaches a persisted effect without stacking.</summary>
    public void RehydrateStatusEffect(CombatStatusEffect effect) => _statusEffects.Add(effect);

    /// <summary>
    /// Advances all durable effects to <paramref name="currentTick"/>: applies any due
    /// DoT/HoT and removes expired effects. Returns what happened (for combat logs).
    /// </summary>
    public IReadOnlyCollection<StatusTickEvent> TickStatusEffects(int currentTick)
    {
        if (_statusEffects.Count == 0)
            return Array.Empty<StatusTickEvent>();

        var events = new List<StatusTickEvent>();

        foreach (var effect in _statusEffects.ToArray())
        {
            if (!IsDefeated && effect.IsPeriodic)
            {
                var amount = effect.ConsumeDueTicks(currentTick, MaxVitality);
                if (amount > 0)
                {
                    if (effect.Kind == StatusEffectKind.DamageOverTime)
                    {
                        var reduced = DotDamageReductionPercent > 0
                            ? Math.Max(0, (int)Math.Round(amount * (1.0 - Math.Min(DotDamageReductionPercent, 100) / 100.0)))
                            : amount;
                        if (reduced > 0)
                            ApplyDamage(reduced);
                        events.Add(new StatusTickEvent(
                            effect.Key, effect.DisplayName, effect.Kind, reduced, false,
                            effect.StackSourceIds));
                    }
                    else if (effect.Kind == StatusEffectKind.HealOverTime && CurrentVitality < MaxVitality)
                    {
                        ApplyHeal(amount);
                        events.Add(new StatusTickEvent(
                            effect.Key, effect.DisplayName, effect.Kind, amount, false,
                            effect.StackSourceIds));
                    }
                    else if (effect.Kind == StatusEffectKind.GuardOverTime)
                    {
                        GainGuard(amount);
                        events.Add(new StatusTickEvent(
                            effect.Key, effect.DisplayName, effect.Kind, amount, false,
                            effect.StackSourceIds));
                    }
                }
            }

            if (effect.IsExpired(currentTick))
            {
                _statusEffects.Remove(effect);
                events.Add(new StatusTickEvent(
                    effect.Key, effect.DisplayName, effect.Kind, 0, true,
                    effect.StackSourceIds));
            }
        }

        return events;
    }

    private int StatModifierFlatSum(CombatStat stat)
        => _statusEffects
            .Where(e => e.Kind == StatusEffectKind.StatModifier && e.Stat == stat && !e.IsMagnitudePercentOfBaseStat)
            .Sum(e => e.Magnitude * e.Stacks);

    // Percent-based StatModifier effects (the default going forward) are summed
    // separately, then applied against the combatant's BASE value for that stat —
    // e.g. two stacked "+10% AttackPower" effects add up to +20% of base, not a
    // compounding ×1.1×1.1.
    private int StatModifierPercentSum(CombatStat stat)
        => _statusEffects
            .Where(e => e.Kind == StatusEffectKind.StatModifier && e.Stat == stat && e.IsMagnitudePercentOfBaseStat)
            .Sum(e => e.Magnitude * e.Stacks);

    private int EffectiveStat(CombatStat stat, int baseValue)
        => baseValue
            + StatModifierFlatSum(stat)
            + (int)Math.Round(baseValue * StatModifierPercentSum(stat) / 100.0);

    // Effective stats = base snapshot + active StatModifier effects (flat and/or
    // percent-of-base; debuffs are negative).
    public int EffectiveAttackPower => Math.Max(0, EffectiveStat(CombatStat.AttackPower, BaseStatSnapshot.AttackPower));
    public int EffectiveDefense => Math.Max(0, EffectiveStat(CombatStat.Defense, BaseStatSnapshot.Defense));
    public int EffectiveSpeed => Math.Max(1, EffectiveStat(CombatStat.Speed, BaseStatSnapshot.Speed));
    public int EffectiveMovement => Math.Max(1, EffectiveStat(CombatStat.Movement, BaseStatSnapshot.Movement));
    public int EffectiveFocus => Math.Max(0, EffectiveStat(CombatStat.Focus, BaseStatSnapshot.Focus));
    public int EffectiveEvasion => Math.Max(0, EffectiveStat(CombatStat.Evasion, 0));
    public int EffectiveMagicAttack => Math.Max(0, EffectiveStat(CombatStat.MagicAttack, BaseStatSnapshot.MagicAttack));
    public int EffectiveMagicDefense => Math.Max(0, EffectiveStat(CombatStat.MagicDefense, BaseStatSnapshot.MagicDefense));

    // Control flags consumed by tactical action validation.
    public bool IsStunned => _statusEffects.Any(e => e.Kind == StatusEffectKind.Stun);
    public bool IsSilenced => _statusEffects.Any(e => e.Kind == StatusEffectKind.Silence);
    public bool HasTacticalSlow => _statusEffects.Any(e =>
        e.Key.StartsWith("equipment.temporal-slow", StringComparison.OrdinalIgnoreCase));
    public bool IsActivationBlocked => IsStunned || IsSilenced;
}
