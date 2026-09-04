using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Dialogue;
using Leds.GameEngine.Domain.Knowledge;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.Domain.Runs;

public sealed class Run
{
    /// <summary>
    /// Base run-bag capacity (SFD "Système d'équipement et sac permanent" § 5) — raised by
    /// permanent backpacks equipped by the player, computed once at StartNew time and passed
    /// in like the other flattened starting stats (attack/defense/speed/focus).
    /// </summary>
    public const int DefaultRunItemCapacity = 6;

    /// <summary>
    /// Nombre maximum de personnages jouables engagés dans une run — le porteur et jusqu'à trois
    /// compagnons.
    /// </summary>
    /// <remarks>
    /// Le tactique en a besoin pour que le déploiement tienne sur une grille. Le roster permanent
    /// du joueur, lui, n'est pas plafonné :
    /// il peut recruter autant de compagnons qu'il veut, seuls les quatre premiers partent.
    /// </remarks>
    public const int MaxPartySize = 4;

    /// <summary>
    /// Minimum number of rooms that must pass between two uses of "Déni permanent" —
    /// see <see cref="CanUseLawDenial"/>.
    /// </summary>
    public const int LawDenialCooldownRooms = 10;

    /// <summary>
    /// Minimum number of rooms that must pass between two uses of "Calice infini" —
    /// see <see cref="CanUseCaliceInfini"/>.
    /// </summary>
    public const int CaliceInfiniCooldownRooms = 1;

    private readonly List<Room> _rooms = [];
    private readonly List<ActivePalaceLaw> _activePalaceLaws = [];
    private readonly List<string> _memoryFragments = [];
    private readonly List<RunJournalEntry> _journalEntries = [];
    private readonly List<RunItem> _runItems = [];
    private readonly List<RunModifier> _runModifiers = [];
    private readonly List<Guid> _suspendedSevereLawModifierIds = [];
    private Combats.Tactical.TacticalCombat? _activeTacticalCombat;
    private ActiveCurse? _activeCurse;
    private RunSnapshot? _roomSnapshot;
    private RunPlayerSnapshot? _playerSnapshot;
    private RunStatus? _preSuspendStatus;
    private readonly Dictionary<string, NpcRelationship> _npcRelationships = new(StringComparer.OrdinalIgnoreCase);
    private string? _activeNpcKey;
    private readonly Dictionary<string, KnowledgeEntry> _knowledgeEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AmbientConversationState> _ambientConversationStates = new(StringComparer.OrdinalIgnoreCase);

    private sealed record RunSnapshot(
        int CurrentHp,
        int Attack,
        int Defense,
        int Speed,
        string[] MemoryFragments,
        ActivePalaceLaw[] ActivePalaceLaws,
        Guid[] RunItemIds,
        Guid[] RunModifierIds);

    public IReadOnlyCollection<ActivePalaceLaw> ActivePalaceLaws =>
    _activePalaceLaws.AsReadOnly();

    public IReadOnlyCollection<string> MemoryFragments =>
        _memoryFragments.AsReadOnly();

    /// <summary>
    /// Auto-written literary log of this run's events (item finds, combat outcomes), only ever
    /// populated when <see cref="JournalEnabled"/> is true. Append-only — unlike other run
    /// resources, it is deliberately NOT part of <see cref="RunSnapshot"/>/<see cref="ExitMidRoom"/>
    /// rollback: it is a historical record of what happened, not a piece of current state. Each
    /// entry is tagged with the room it happened in (SFD frontend "Carnet de bord" § 1 page = 1 room).
    /// </summary>
    public IReadOnlyCollection<RunJournalEntry> JournalEntries => _journalEntries.AsReadOnly();

    /// <summary>
    /// True when the player owns the "Carnet de bord" permanent item — computed once at
    /// <see cref="StartNew"/> time from the player's profile, like the other flattened starting
    /// stats/bonuses.
    /// </summary>
    public bool JournalEnabled { get; private set; }

    /// <summary>
    /// True when the player owns the "Déni permanent" permanent item — computed once at
    /// <see cref="StartNew"/> time from the player's profile, like <see cref="JournalEnabled"/>.
    /// </summary>
    public bool LawDenialEnabled { get; private set; }

    /// <summary>
    /// <see cref="CurrentRoomIndex"/> at which "Déni permanent" was last used this run, or
    /// <c>null</c> if never used. Gates <see cref="CanUseLawDenial"/> via <see cref="LawDenialCooldownRooms"/>.
    /// </summary>
    public int? LawDenialLastUsedRoomIndex { get; private set; }

    /// <summary>
    /// True when the player can currently revoke an active Palace Law with "Déni permanent":
    /// owns the item, and either never used it this run or has progressed at least
    /// <see cref="LawDenialCooldownRooms"/> rooms since the last use.
    /// </summary>
    public bool CanUseLawDenial =>
        LawDenialEnabled &&
        (LawDenialLastUsedRoomIndex is null || CurrentRoomIndex - LawDenialLastUsedRoomIndex.Value >= LawDenialCooldownRooms);

    /// <summary>
    /// True when the player owns the "Calice infini" permanent item — computed once at
    /// <see cref="StartNew"/> time from the player's profile, like <see cref="LawDenialEnabled"/>.
    /// </summary>
    public bool CaliceInfiniEnabled { get; private set; }

    /// <summary>
    /// <see cref="CurrentRoomIndex"/> at which "Calice infini" was last used this run, or
    /// <c>null</c> if never used. Gates <see cref="CanUseCaliceInfini"/> via <see cref="CaliceInfiniCooldownRooms"/>.
    /// </summary>
    public int? CaliceInfiniLastUsedRoomIndex { get; private set; }

    /// <summary>
    /// True when the player can currently use "Calice infini": owns the item, and either
    /// never used it this run or has progressed at least one room since the last use.
    /// </summary>
    public bool CanUseCaliceInfini =>
        CaliceInfiniEnabled &&
        (CaliceInfiniLastUsedRoomIndex is null || CurrentRoomIndex > CaliceInfiniLastUsedRoomIndex.Value);

    /// <summary>
    /// Number of rooms per "étage" (floor), used only by floor-scoped laws and modifiers.
    /// It has no effect on boss or Him'Lit encounter frequency.
    /// </summary>
    public const int FloorLengthInRooms = 10;

    /// <summary>
    /// Zero-based index of the current "étage" (floor) — the granularity used by the
    /// Compendium des Lois du Palais for "1 promulgation garantie par nouvel étage" and by
    /// <see cref="RunModifierDuration.UntilFloorEnds"/>.
    /// </summary>
    public int FloorIndex => CurrentRoomIndex / FloorLengthInRooms;

    /// <summary>
    /// <see cref="FloorIndex"/> at which a law was last promulgated (via a guaranteed
    /// new-floor draw or the 20%-per-room roll) — used by the caller to know whether this
    /// floor's guaranteed promulgation has already happened.
    /// </summary>
    public int? LastPromulgationFloorIndex { get; private set; }

    /// <summary>
    /// "Loi de l'Oubli Partiel" (law.oubli-partiel): the skill key forgotten for the
    /// rest of the floor, if the law is currently active. Picked once at promulgation
    /// time by <see cref="PickForgottenSkill"/> (not stored on the RunModifier itself —
    /// RunModifier.Value is a plain double, no string payload). Cleared by
    /// <see cref="ConsumeFloorEndModifiers"/> once the backing modifier expires.
    /// </summary>
    public string? ForgottenSkillKey { get; private set; }

    /// <summary>
    /// Number of currently active "Sévère" laws at or above which the Soupape rule kicks in.
    /// </summary>
    public const int SoupapeSevereThreshold = 3;

    /// <summary>
    /// The Soupape rule: true once <see cref="SoupapeSevereThreshold"/> or more active laws
    /// are <see cref="PalaceLawPolarity.Severe"/>. The CALLER — whoever draws the next law to
    /// promulgate — must then restrict that draw to <see cref="PalaceLawPolarity.Clemente"/> or
    /// <see cref="PalaceLawPolarity.DoubleTranchant"/> laws; <see cref="PromulgateLaw"/> itself
    /// receives an already-drawn law and does not re-roll.
    /// </summary>
    public bool ShouldForceCompliantPromulgation =>
        _activePalaceLaws.Count(law => law.Polarity == PalaceLawPolarity.Severe) >= SoupapeSevereThreshold;

    public IReadOnlyCollection<RunItem> RunItems =>
        _runItems.Where(item => !item.IsOnGround).ToArray();

    public IReadOnlyCollection<RunItem> GroundItems =>
        _runItems.Where(item => item.IsOnGround).ToArray();

    /// <summary>Persistence-only view containing inventory and room-local ground loot.</summary>
    public IReadOnlyCollection<RunItem> PersistedRunItems => _runItems.AsReadOnly();

    public int RunItemCapacity { get; }

    /// <summary>
    /// All run modifiers — both active and already-consumed.
    /// Filter by <see cref="RunModifier.IsConsumed"/> as needed.
    /// </summary>
    public IReadOnlyCollection<RunModifier> RunModifiers => _runModifiers.AsReadOnly();

    /// <summary>
    /// Ids of the Sévère-law modifiers currently paused by "Loi du Répit" (law.repit)'s
    /// ACCALMIE — see <see cref="SuspendActiveSevereLawModifiers"/>. Persisted so the
    /// suspension survives across command calls; resumed in <see cref="MoveToNextRoom"/>
    /// (leaving the room) and <see cref="ExitMidRoom"/> (rolling back to before it started).
    /// </summary>
    public IReadOnlyCollection<Guid> SuspendedSevereLawModifierIds => _suspendedSevereLawModifierIds.AsReadOnly();

    private Run(
        RunId id,
        Guid playerId,
        string seed,
        string generatorVersion,
        string markovMatrixVersion,
        RunStatus status,
        Room initialRoom,
        DateTimeOffset startedAt,
        int maxHp,
        int currentHp,
        int attack,
        int defense,
        int speed,
        int focus,
        int currentRoomIndex = 0,
        CombatId? activeCombatId = null,
        RewardOfferId? pendingRewardOfferId = null,
        int runItemCapacity = DefaultRunItemCapacity,
        IReadOnlyDictionary<string, int>? typedDamageReductions = null,
        int hitChanceBonusPercent = 0,
        int dotDurationReductionPercent = 0,
        int dotDamageReductionPercent = 0,
        int dotDamageBonusPercent = 0,
        int magicDamageBonusPercent = 0,
        int magicDamageReductionPercent = 0,
        int criticalChanceBonusPercent = 0,
        int guardBonusPercent = 0,
        bool journalEnabled = false,
        bool lawDenialEnabled = false,
        int? lawDenialLastUsedRoomIndex = null,
        int reputationGainBonusPercent = 0,
        bool himLitProtectionEnabled = false,
        int healingBonusPercent = 0,
        bool caliceInfiniEnabled = false,
        int? caliceInfiniLastUsedRoomIndex = null,
        int magicAttack = 0,
        int magicDefense = 0,
        int? lastPromulgationFloorIndex = null,
        string? forgottenSkillKey = null,
        EmotionalAffinityMatrixSnapshot? emotionalAffinityMatrix = null,
        RunOutcome? outcome = null,
        long revision = 0,
        TechnicalRecoveryState technicalRecoveryState = TechnicalRecoveryState.None)
    {
        Id = id;
        PlayerId = playerId;
        Seed = seed;
        GeneratorVersion = generatorVersion;
        MarkovMatrixVersion = markovMatrixVersion;
        Status = status;
        Outcome = outcome;
        Revision = revision;
        TechnicalRecoveryState = technicalRecoveryState;
        CurrentRoomId = initialRoom.Id;
        StartedAt = startedAt;
        MaxHp = maxHp;
        CurrentHp = currentHp;
        Attack = attack;
        Defense = defense;
        Speed = speed;
        Focus = focus;
        MagicAttack = magicAttack;
        MagicDefense = magicDefense;
        CurrentRoomIndex = currentRoomIndex;
        ActiveCombatId = activeCombatId;
        PendingRewardOfferId = pendingRewardOfferId;
        RunItemCapacity = runItemCapacity;
        TypedDamageReductions = typedDamageReductions ?? new Dictionary<string, int>();
        HitChanceBonusPercent = hitChanceBonusPercent;
        DotDurationReductionPercent = dotDurationReductionPercent;
        DotDamageReductionPercent = dotDamageReductionPercent;
        DotDamageBonusPercent = dotDamageBonusPercent;
        MagicDamageBonusPercent = magicDamageBonusPercent;
        MagicDamageReductionPercent = magicDamageReductionPercent;
        CriticalChanceBonusPercent = criticalChanceBonusPercent;
        GuardBonusPercent = guardBonusPercent;
        JournalEnabled = journalEnabled;
        LawDenialEnabled = lawDenialEnabled;
        LawDenialLastUsedRoomIndex = lawDenialLastUsedRoomIndex;
        ReputationGainBonusPercent = reputationGainBonusPercent;
        HimLitProtectionEnabled = himLitProtectionEnabled;
        HealingBonusPercent = healingBonusPercent;
        CaliceInfiniEnabled = caliceInfiniEnabled;
        CaliceInfiniLastUsedRoomIndex = caliceInfiniLastUsedRoomIndex;
        LastPromulgationFloorIndex = lastPromulgationFloorIndex;
        ForgottenSkillKey = forgottenSkillKey;
        EmotionalAffinityMatrix = emotionalAffinityMatrix
            ?? throw new DomainException("A Catalog emotional affinity matrix snapshot is required.");

        if ((Status == RunStatus.Resolved) != Outcome.HasValue)
        {
            throw new DomainException(
                "A resolved run requires an outcome and an open run cannot have one.");
        }

        _rooms.Add(initialRoom);
    }

    public RunId Id { get; }

    public Guid PlayerId { get; }

    public string Seed { get; }

    public string GeneratorVersion { get; }

    public string MarkovMatrixVersion { get; }

    public EmotionalAffinityMatrixSnapshot EmotionalAffinityMatrix { get; }

    public RunProgressionMode ProgressionMode { get; private set; } = RunProgressionMode.Standard;
    public StoryDifficulty? StoryDifficulty { get; private set; }
    public DifficultyLevel? DifficultyLevel { get; private set; }
    public StoryRunOverlay? StoryOverlay { get; private set; }

    public void ConfigureStoryRun(StoryRunOverlay overlay)
    {
        EnsureActive();
        ProgressionMode = RunProgressionMode.Story;
        StoryDifficulty = global::Leds.GameEngine.Domain.Runs.StoryDifficulty.Canonical;
        DifficultyLevel = null;
        StoryOverlay = overlay;
    }

    public void ConfigureDifficultyRun(DifficultyLevel difficultyLevel)
    {
        EnsureActive();
        ProgressionMode = RunProgressionMode.Standard;
        StoryDifficulty = null;
        DifficultyLevel = difficultyLevel;
        StoryOverlay = null;
    }

    public void RestoreProgressionMode(
        RunProgressionMode mode,
        StoryDifficulty? storyDifficulty,
        int? difficultyLevel,
        StoryRunOverlay? storyOverlay)
    {
        ProgressionMode = mode;
        StoryDifficulty = storyDifficulty;
        DifficultyLevel = difficultyLevel.HasValue
            ? global::Leds.GameEngine.Domain.Runs.DifficultyLevel.Create(difficultyLevel.Value)
            : null;
        StoryOverlay = storyOverlay;
    }

    public ContentVersionSet ContentVersions => new(
        GeneratorVersion,
        MarkovMatrixVersion,
        EmotionalAffinityMatrix.Version);

    public RunStatus Status { get; private set; }

    public RunOutcome? Outcome { get; private set; }

    public long Revision { get; private set; }

    public TechnicalRecoveryState TechnicalRecoveryState { get; private set; }

    public bool IsResolved => Status == RunStatus.Resolved;

    public void AcceptPersistedRevision(long revision)
    {
        if (revision <= Revision)
            throw new DomainException("Persisted run revision must increase.");

        Revision = revision;
    }

    public RoomId CurrentRoomId { get; private set; }

    public CombatId? ActiveCombatId { get; private set; }

    public bool HasActiveCombat =>
        ActiveCombatId.HasValue || _activeTacticalCombat is not null;

    /// <summary>
    /// Projects every physical party member's current combat resources back to the durable run
    /// snapshot befor