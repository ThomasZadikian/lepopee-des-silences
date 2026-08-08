using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Combats.Typing;
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
    /// Number of rooms per "étage" (floor) — matches the Him'Lit boss recurrence interval
    /// (services/game-engine's BossInterval, Infrastructure-layer, can't be referenced directly
    /// from Domain). Kept as its own constant here rather than shared across the layer boundary.
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
        EmotionalAffinityMatrixSnapshot? emotionalAffinityMatrix = null)
    {
        Id = id;
        PlayerId = playerId;
        Seed = seed;
        GeneratorVersion = generatorVersion;
        MarkovMatrixVersion = markovMatrixVersion;
        Status = status;
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

        _rooms.Add(initialRoom);
    }

    public RunId Id { get; }

    public Guid PlayerId { get; }

    public string Seed { get; }

    public string GeneratorVersion { get; }

    public string MarkovMatrixVersion { get; }

    public EmotionalAffinityMatrixSnapshot EmotionalAffinityMatrix { get; }

    public RunStatus Status { get; private set; }

    public RoomId CurrentRoomId { get; private set; }

    public CombatId? ActiveCombatId { get; private set; }

    public bool HasActiveCombat =>
        ActiveCombatId.HasValue || _activeTacticalCombat is not null;

    /// <summary>
    /// Le combat tactique en cours, le cas échéant. Renseigné par <see cref="StartTacticalCombat"/>.
    /// </summary>
    /// <remarks>
    /// L'emplacement tactique est le seul utilisé par le jeu. L'ancien agrégat est encore
    /// relu par la couche de persistance le temps que les données historiques soient purgées.
    /// </remarks>
    public Combats.Tactical.TacticalCombat? ActiveTacticalCombat => _activeTacticalCombat;

    public RewardOfferId? PendingRewardOfferId { get; private set; }

    public bool HasPendingRewardOffer => PendingRewardOfferId.HasValue;

    /// <summary>
    /// Nothing in progress: the run is <see cref="RunStatus.Active"/>, no active combat, no
    /// pending reward offer, and the current room is back in free exploration (not mid
    /// node-selection/resolution). No dependency on the room's boss anymore — it stopped
    /// gating anything (see <see cref="ConfirmRoomExit"/>) — so this is the single rule both
    /// <see cref="SaveAndExit"/> and abandoning the run use to decide "can I leave cleanly
    /// right now."
    /// </summary>
    public bool IsAtSafePoint =>
        Status == RunStatus.Active
        && !HasActiveCombat
        && !HasPendingRewardOffer
        && CurrentRoom.State == RoomState.Active;

    /// <summary>
    /// The active curse applied to the run, if any.
    /// </summary>
    public ActiveCurse? ActiveCurse => _activeCurse;

    /// <summary>
    /// Immutable player snapshot captured at run creation.
    /// Null only for runs created before data-model-0.1 migration.
    /// </summary>
    /// 
        // ── NPC encounters (npc-system-sfd-0.1) ──────────────────────────────────

    /// <summary>Key of the NPC currently being talked to, if any.</summary>
    public string? ActiveNpcKey => _activeNpcKey;

    /// <summary>All NPC relationships met during this run (L2 memory; L1 uses the active one).</summary>
    public IReadOnlyCollection<NpcRelationship> NpcRelationships =>
        _npcRelationships.Values.ToArray();

    public NpcRelationship? ActiveNpcRelationship =>
        _activeNpcKey is null ? null : GetNpcRelationship(_activeNpcKey);

    public NpcRelationship? GetNpcRelationship(string npcKey) =>
        _npcRelationships.TryGetValue(npcKey, out var relationship) ? relationship : null;

    /// <summary>
    /// Marks an NPC encounter active. A recurring NPC (already met) resumes its existing
    /// relationship and registers a new meeting; otherwise a fresh relationship begins.
    /// The active dialogue node is initialised lazily by the resolver from the NPC graph.
    /// </summary>
    public NpcRelationship BeginOrResumeNpcEncounter(string npcKey)
    {
        _activeNpcKey = npcKey;

        if (_npcRelationships.TryGetValue(npcKey, out var existing))
        {
            existing.RegisterNewMeeting();
            return existing;
        }

        var relationship = NpcRelationship.Begin(npcKey, entryNodeKey: null);
        _npcRelationships[npcKey] = relationship;
        return relationship;
    }

    public void EndNpcEncounter() => _activeNpcKey = null;

    /// <summary>
    /// Adjusts (creating if needed) the relationship score with an NPC OTHER than the
    /// one currently active — e.g. Araran's legendary offering vouching for the player
    /// with Tovma and Mané. Unlike <see cref="BeginOrResumeNpcEncounter"/>, this never
    /// changes <c>ActiveNpcKey</c> and does not count as a meeting.
    /// </summary>
    public void AdjustNpcRelationshipScore(string npcKey, int delta)
    {
        if (!_npcRelationships.TryGetValue(npcKey, out var relationship))
        {
            relationship = NpcRelationship.Begin(npcKey, entryNodeKey: null);
            _npcRelationships[npcKey] = relationship;
        }

        relationship.AdjustScore(ScaleReputationGain(delta, npcKey));
    }

    /// <summary>
    /// Threshold (on the OTHER npc's relationship score) above/below which a narrative
    /// pair-modifier in <see cref="ScaleReputationGain"/> kicks in — reuses the same
    /// "rare" tier value as every NpcOffering's rare-gift gate, for a consistent read on
    /// "this other relationship is clearly established" either way.
    /// </summary>
    private const int NarrativePairAffinityThreshold = 250;

    /// <summary>
    /// Applies <see cref="ReputationGainBonusPercent"/> plus a handful of authored,
    /// NPC-pair-specific modifiers to a relationship-score delta — social dynamics
    /// between NPCs that indirectly color how easily the player can win one of them over:
    /// <list type="bullet">
    /// <item>Homoncule → Forgeron: his proudest creation being liked makes HIM warm to the
    /// player faster (+20%, one-directional).</item>
    /// <item>Homoncule &lt;-&gt; Enfant: they despise each other, so being close to one makes
    /// the other harder to win over (-30%, mutual).</item>
    /// <item>Iris: "juge à travers les yeux d'Ethan" — if Ethan has come to dislike the
    /// player, Iris struggles to like them too (-30%).</item>
    /// <item>Araran / Tovma / Mané: a mirrored trio — falling out with any one of the
    /// three makes the other two harder to win over (-30% each).</item>
    /// </list>
    /// Only the base bonus and NPC-pair modifiers above are scoped to POSITIVE gains —
    /// negative deltas (e.g. transgressions) normally pass through unchanged, EXCEPT
    /// under "Loi du Nom Retenu" (RunModifierType.ReputationChangeDoubled), which doubles
    /// gains AND losses alike for the floor. NpcRelationship.AdjustScore already floors
    /// Elise's score against any decrease, so doubling a penalty destined for her is a
    /// no-op there, matching the law's own carve-out for her.
    /// </summary>
    public int ScaleReputationGain(int delta, string? targetNpcKey = null)
    {
        var doubled = GetActiveModifiers(RunModifierType.ReputationChangeDoubled).Count > 0;

        if (delta <= 0)
        {
            return doubled ? delta * 2 : delta;
        }

        var percent = ReputationGainBonusPercent;
        percent += NarrativePairModifierPercent(targetNpcKey);

        var scaled = percent == 0 ? delta : (int)Math.Round(delta * (100 + percent) / 100.0);
        return doubled ? scaled * 2 : scaled;
    }

    private int NarrativePairModifierPercent(string? targetNpcKey)
    {
        if (string.IsNullOrWhiteSpace(targetNpcKey))
        {
            return 0;
        }

        int OtherScore(string npcKey) => GetNpcRelationship(npcKey)?.RelationshipScore ?? 0;
        bool Likes(string npcKey) => OtherScore(npcKey) >= NarrativePairAffinityThreshold;
        bool Dislikes(string npcKey) => OtherScore(npcKey) < 0;

        return targetNpcKey.ToLowerInvariant() switch
        {
            "npc.forgeron" => Likes("npc.homoncule") ? 20 : 0,
            "npc.homoncule" => Likes("npc.enfant") ? -30 : 0,
            "npc.enfant" => Likes("npc.homoncule") ? -30 : 0,
            "npc.iris" => Dislikes("npc.ethan") ? -30 : 0,
            "npc.araran" => Dislikes("npc.tovma") || Dislikes("npc.mane") ? -30 : 0,
            "npc.tovma" => Dislikes("npc.araran") || Dislikes("npc.mane") ? -30 : 0,
            "npc.mane" => Dislikes("npc.araran") || Dislikes("npc.tovma") ? -30 : 0,
            _ => 0
        };
    }

    /// <summary>Rehydration hook for persistence (Wave 5).</summary>
    public void RehydrateNpcRelationship(NpcRelationship relationship)
    {
        _npcRelationships[relationship.NpcKey] = relationship;
    }

    /// <summary>Rehydration hook for persistence (Wave 5).</summary>
    public void RehydrateActiveNpcKey(string? activeNpcKey)
    {
        _activeNpcKey = activeNpcKey;
    }
    public RunPlayerSnapshot? PlayerSnapshot => _playerSnapshot;

    /// <summary>
    /// Attaches a player snapshot to this run.
    /// Called once at run creation; will throw if already set.
    /// </summary>
    public void AttachPlayerSnapshot(RunPlayerSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (_playerSnapshot is not null)
        {
            throw new DomainException("Player snapshot is already attached to this run.");
        }

        _playerSnapshot = snapshot;
    }

    public PlayerRuntimeState PlayerState { get; private set; } = null!;

    /// <summary>
    /// Grimoire mid-run resync ("Valider les choix"): replaces the protagonist's
    /// effective combat skills so the next combat casts with the freshly-validated
    /// selection instead of the loadout frozen at StartRun.
    /// </summary>
    public void ReplacePlayerSkills(IReadOnlyCollection<PlayerRuntimeSkill> skills)
    {
        PlayerState.ReplaceSkills(skills);
    }

    /// <summary>
    /// Grimoire mid-run resync ("Valider les choix") for a companion: replaces the
    /// matching <see cref="RunCharacterSnapshot"/>'s effective combat skills.
    /// </summary>
    public void ReplaceCharacterSkills(Guid characterId, IReadOnlyCollection<RunCharacterSkillSnapshot> skills)
    {
        var character = PlayerSnapshot?.Characters.FirstOrDefault(c => c.CharacterId == characterId)
            ?? throw new DomainException($"Character '{characterId}' was not found in this run's player snapshot.");

        character.ReplaceSkills(skills);
    }

    private static readonly HashSet<string> GrimoireTemporarySkillKeys =
    [
        "skill.temp.deluge-mineur",
        "skill.temp.ecriture-appliquee",
        "skill.temp.souffle-emprunte",
        "canon.skill.priere-aspiration",
        "skill.temp.construction-ephemere"
    ];

    /// <summary>
    /// Consumes a grimoire outside combat and replaces the character's dedicated
    /// third temporary slot. The two ordinary additional slots are untouched.
    /// </summary>
    public bool GrantGrimoireSkill(
        RunItemId itemId,
        Guid characterId,
        PlayerRuntimeSkill runtimeSkill,
        RunCharacterSkillSnapshot snapshotSkill)
    {
        if (HasActiveCombat)
            throw new DomainException("A grimoire can only be read outside combat.");

        var item = _runItems.FirstOrDefault(candidate => candidate.Id == itemId)
            ?? throw new DomainException($"Item '{itemId.Value}' not found in run inventory.");
        if (item.Type != RunItemType.Grimoire
            || item.EffectType != RunItemEffectType.GrantTemporarySkill)
        {
            throw new DomainException($"Item '{item.DefinitionKey}' is not a usable grimoire.");
        }

        var character = PlayerSnapshot?.Characters.FirstOrDefault(c => c.CharacterId == characterId)
            ?? throw new DomainException($"Character '{characterId}' was not found in this run.");

        var characterSkills = character.Skills
            .Where(skill =>
                !string.Equals(skill.TemporarySlot, "Grimoire", StringComparison.OrdinalIgnoreCase)
                && !GrimoireTemporarySkillKeys.Contains(skill.SkillDefinitionKey))
            .Append(snapshotSkill)
            .ToArray();
        character.ReplaceSkills(characterSkills);

        if (PlayerSnapshot!.Characters.First().CharacterId == characterId)
        {
            var playerSkills = PlayerState.Skills
                .Where(skill => !GrimoireTemporarySkillKeys.Contains(skill.Key))
                .Append(runtimeSkill)
                .ToArray();
            PlayerState.ReplaceSkills(playerSkills);
        }

        item.ConsumeOne();
        return item.Quantity == 0;
    }

    public bool ConsumeGrimoire(RunItemId itemId)
    {
        if (HasActiveCombat)
            throw new DomainException("A grimoire can only be read outside combat.");

        var item = _runItems.FirstOrDefault(candidate => candidate.Id == itemId)
            ?? throw new DomainException($"Item '{itemId.Value}' not found in run inventory.");
        if (item.Type != RunItemType.Grimoire)
            throw new DomainException($"Item '{item.DefinitionKey}' is not a grimoire.");

        item.ConsumeOne();
        return item.Quantity == 0;
    }

    public int MaxHp { get; private set; }

    public int CurrentHp { get; private set; }

    public int Attack { get; private set; }

    public int Defense { get; private set; }

    public int Speed { get; private set; }

    /// <summary>
    /// Character Focus stat (drives critical-hit chance in combat). Sourced from the
    /// main character at run start; can be raised mid-run by <see cref="ReplacePlayerStats"/>.
    /// </summary>
    public int Focus { get; private set; }

    public int MagicAttack { get; private set; }

    public int MagicDefense { get; private set; }

    /// <summary>
    /// Stat-point mid-run resync ("Valider les choix"): applies the protagonist's
    /// freshly-computed effective stats. MaxHp/CurrentHp are kept in sync with
    /// <see cref="PlayerState"/>'s vitality (the value combat actually reads) so the
    /// two representations don't drift.
    /// </summary>
    public void ReplacePlayerStats(
        int maxVitality,
        int maxMana,
        int charge,
        int attack,
        int defense,
        int speed,
        int focus,
        int magicAttack,
        int magicDefense,
        int guardBonusPercent)
    {
        PlayerState.ReplaceEffectiveStats(maxVitality, maxMana, charge);

        MaxHp = maxVitality;
        CurrentHp = maxVitality;
        Attack = attack;
        Defense = defense;
        Speed = speed;
        Focus = focus;
        MagicAttack = magicAttack;
        MagicDefense = magicDefense;
        GuardBonusPercent = guardBonusPercent;
    }

    /// <summary>
    /// Stat-point mid-run resync ("Valider les choix") for a companion (or the
    /// protagonist's own mirror entry, index 0): replaces the matching
    /// <see cref="RunCharacterSnapshot"/>'s stat block.
    /// </summary>
    public void ReplaceCharacterStats(
        Guid characterId,
        int maxVitality,
        int attackPower,
        int defense,
        int startingGuard,
        int speed,
        int initiative,
        int focus,
        int mana,
        int charge,
        int magicAttack,
        int magicDefense,
        int movement = 4,
        IReadOnlyCollection<string>? equippedItemKeys = null)
    {
        var character = PlayerSnapshot?.Characters.FirstOrDefault(c => c.CharacterId == characterId)
            ?? throw new DomainException($"Character '{characterId}' was not found in this run's player snapshot.");

        character.StatBlock.ReplaceStats(
            maxVitality, attackPower, defense, startingGuard, speed,
            initiative, focus, mana, charge, magicAttack, magicDefense, movement);

        if (equippedItemKeys is not null)
            character.ReplaceEquippedItemKeys(equippedItemKeys);
    }

    /// <summary>
    /// Equipment-driven typed damage reductions (EmotionalType name -> percent 0-100),
    /// computed once at run start from equipped items (SFD equipment §8) and immutable
    /// for the run's lifetime, same as Attack/Defense/Speed/Focus.
    /// </summary>
    public IReadOnlyDictionary<string, int> TypedDamageReductions { get; }

    /// <summary>
    /// Equipment-driven percentage points added to the protagonist's hit chance
    /// (e.g. Lunettes d'érudit: +10%). Computed once at run start, immutable for
    /// the run's lifetime, same as TypedDamageReductions.
    /// </summary>
    public int HitChanceBonusPercent { get; }

    /// <summary>
    /// Equipment-driven percentage (0-100) by which incoming DamageOverTime effects
    /// have their duration/per-tick damage reduced (e.g. Main de Khasma).
    /// </summary>
    public int DotDurationReductionPercent { get; }
    public int DotDamageReductionPercent { get; }

    /// <summary>
    /// Equipment-driven percentage points added to the DAMAGE DEALT by the
    /// protagonist's DamageOverTime effects (e.g. l'Écrivain's Plume d'écrivain).
    /// Computed once at run start, immutable for the run's lifetime.
    /// </summary>
    public int DotDamageBonusPercent { get; }

    /// <summary>
    /// Equipment-driven percentage points added to / subtracted from Magic-category
    /// skill damage (e.g. Pomenian's monocle). Computed once at run start, immutable
    /// for the run's lifetime, same as HitChanceBonusPercent.
    /// </summary>
    public int MagicDamageBonusPercent { get; }
    public int MagicDamageReductionPercent { get; }
    public int CriticalChanceBonusPercent { get; }

    /// <summary>
    /// Computed once at run start from equipped items; can be raised mid-run by
    /// <see cref="ReplacePlayerStats"/> (equipment mid-run resync).
    /// </summary>
    public int GuardBonusPercent { get; private set; }

    /// <summary>
    /// Percentage bonus applied to positive NPC relationship-score gains (e.g. Mina's
    /// "Peluche de Mina", owned — not equipped). Never applied to penalties (transgressions).
    /// Computed once at run start, immutable for the run's lifetime.
    /// </summary>
    public int ReputationGainBonusPercent { get; }

    /// <summary>
    /// True when the player owns Mina's legendary "Protection de Him'Lit" — computed once at
    /// <see cref="StartNew"/> time from the player's profile, like <see cref="JournalEnabled"/>.
    /// Drives both a tighter Him'Lit boss-room interval (room generation) and an innate combat
    /// debuff/buff bundle applied to the protagonist at combat start (see CombatFactory).
    /// </summary>
    public bool HimLitProtectionEnabled { get; }

    /// <summary>
    /// Equipment-driven percentage points added to ALL healing effects (skills, items,
    /// in and out of combat) — e.g. Majordome's legendary "La tasse du majordome": +15%.
    /// Computed once at run start, immutable for the run's lifetime.
    /// </summary>
    public int HealingBonusPercent { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>
    /// The timestamp at which the run was saved and exited by the player.
    /// Set when <see cref="SaveAndExit"/> is called.
    /// <c>null</c> when the run has never been suspended.
    /// </summary>
    public DateTimeOffset? SavedAt { get; private set; }

    /// <summary>
    /// Zero-based index of the current room in the infinite run sequence.
    /// Threshold is always index 0. Incremented by MoveToNextRoom (future).
    /// Display as <c>CurrentRoomIndex + 1</c> for player-facing "Salle N" labels.
    /// </summary>
    /// <remarks>
    /// Terminology guide:
    /// <list type="bullet">
    ///   <item><c>CurrentRoomIndex</c> — which room in the run (0, 1, 2 …)</item>
    ///   <item><c>Room.CurrentNodeDepth</c> — how far into the current RoomMap's rows</item>
    ///   <item><c>MapNode.Row</c> — a node's fixed position within the RoomMap layout</item>
    /// </list>
    /// Do not use <c>CurrentDepth</c> or <c>Room.Depth</c> to display room count to players.
    /// </remarks>
    public int CurrentRoomIndex { get; private set; }

    /// <summary>
    /// Depth of the current room within the run, derived from <see cref="Room.Depth"/>.
    /// Used internally for room-sequencing validation (e.g. <see cref="MoveToNextRoom"/>).
    /// Not intended as a player-facing room counter — use <see cref="CurrentRoomIndex"/> instead.
    /// </summary>
    public int CurrentDepth => CurrentRoom.Depth;

    public Room CurrentRoom => _rooms.Single(room => room.Id == CurrentRoomId);

    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    public static Run StartNew(
        Guid playerId,
        string seed,
        string generatorVersion,
        string markovMatrixVersion,
        Room initialRoom,
        DateTimeOffset startedAt,
        int maxHp = 40,
        int currentHp = 40,
        int attack = 12,
        int defense = 6,
        int speed = 10,
        IReadOnlyCollection<PlayerRuntimeSkill>? playerSkills = null,
        int focus = 0,
        int mana = 0,
        int? maxMana = null,
        int charge = 0,
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
        int reputationGainBonusPercent = 0,
        bool himLitProtectionEnabled = false,
        int healingBonusPercent = 0,
        bool caliceInfiniEnabled = false,
        int magicAttack = 0,
        int magicDefense = 0,
        EmotionalAffinityMatrixSnapshot? emotionalAffinityMatrix = null)
    {
        if (playerId == Guid.Empty)
        {
            throw new DomainException("Player id is required.");
        }

        if (string.IsNullOrWhiteSpace(seed))
        {
            throw new DomainException("Run seed is required.");
        }

        if (string.IsNullOrWhiteSpace(generatorVersion))
        {
            throw new DomainException("Generator version is required.");
        }

        if (string.IsNullOrWhiteSpace(markovMatrixVersion))
        {
            throw new DomainException("Markov matrix version is required.");
        }

        if (initialRoom.Depth != 0)
        {
            throw new DomainException("Initial room depth must be 0.");
        }

        if (initialRoom.CurrentNodeDepth != 0)
        {
            throw new DomainException("Initial room node depth must be 0.");
        }

        if (initialRoom.ContentNodeCount is < 6 or > 30)
        {
            throw new DomainException("A new run must start with a room containing between 6 and 30 nodes.");
        }

        if (initialRoom.Nodes.Count(node => node.IsBoss) != 1)
        {
            throw new DomainException("A new run must start with exactly one room boss node.");
        }

        if (currentHp <= 0)
        {
            throw new DomainException("Current HP must be greater than 0.");
        }

        if (maxHp <= 0)
        {
            throw new DomainException("Max HP must be greater than 0.");
        }

        if (currentHp > maxHp)
        {
            throw new DomainException("Current HP cannot exceed max HP.");
        }

        if (attack <= 0)
        {
            throw new DomainException("Attack must be greater than 0.");
        }

        if (defense < 0)
        {
            throw new DomainException("Defense cannot be negative.");
        }

        if (speed <= 0)
        {
            throw new DomainException("Speed must be greater than 0.");
        }

        var run = new Run(
            RunId.New(),
            playerId,
            seed.Trim(),
            generatorVersion.Trim(),
            markovMatrixVersion.Trim(),
            RunStatus.Active,
            initialRoom,
            startedAt,
            maxHp,
            currentHp,
            attack,
            defense,
            speed,
            focus,
            runItemCapacity: runItemCapacity,
            typedDamageReductions: typedDamageReductions,
            hitChanceBonusPercent: hitChanceBonusPercent,
            dotDurationReductionPercent: dotDurationReductionPercent,
            dotDamageReductionPercent: dotDamageReductionPercent,
            dotDamageBonusPercent: dotDamageBonusPercent,
            magicDamageBonusPercent: magicDamageBonusPercent,
            magicDamageReductionPercent: magicDamageReductionPercent,
            criticalChanceBonusPercent: criticalChanceBonusPercent,
            guardBonusPercent: guardBonusPercent,
            journalEnabled: journalEnabled,
            lawDenialEnabled: lawDenialEnabled,
            reputationGainBonusPercent: reputationGainBonusPercent,
            himLitProtectionEnabled: himLitProtectionEnabled,
            healingBonusPercent: healingBonusPercent,
            caliceInfiniEnabled: caliceInfiniEnabled,
            magicAttack: magicAttack,
            magicDefense: magicDefense,
            emotionalAffinityMatrix: emotionalAffinityMatrix);

        run.PlayerState = PlayerRuntimeState.Create(
            maxVitality: maxHp,
            skills: playerSkills ?? CreateDefaultPlayerSkills(),
            currentVitality: currentHp,
            mana: mana,
            charge: charge,
            maxMana: maxMana);

        run._roomSnapshot = run.CreateSnapshot();

        return run;
    }

    private static IReadOnlyCollection<PlayerRuntimeSkill> CreateDefaultPlayerSkills()
    {
        return
        [
            PlayerRuntimeSkill.Create(
                key: "skill.basic.strike",
                displayName: "Frappe",
                skillType: "Damage",
                targetingType: "SingleEnemy",
                effectType: "Damage",
                manaCost: 0,
                chargeCost: 0,
                basePower: 10,
                emotionalRegister: "Neutral"),
            PlayerRuntimeSkill.Create(
                key: "skill.basic.guard",
                displayName: "Garde",
                skillType: "Defense",
                targetingType: "Self",
                effectType: "Guard",
                manaCost: 0,
                chargeCost: 0,
                basePower: 5,
                emotionalRegister: "Neutral")
        ];
    }

    /// <summary>
    /// "Provoquer le destin" — raises an available combat-flavored node's danger by one
    /// tier before the player commits to it, in exchange for better rewards. Delegates
    /// entirely to <see cref="MapNode.RaiseRisk"/> for its guards (combat-flavored,
    /// still Available, not already Fatal).
    /// </summary>
    public void RaiseNodeRisk(Guid nodeId)
    {
        EnsureActive();

        CurrentRoom.GetNode(new NodeId(nodeId)).RaiseRisk();
    }

    /// <summary>
    /// Room-wide difficulty selector: since combat nodes trigger on contact (no per-node
    /// confirmation step in grid exploration), this lets the player pick a danger tier once
    /// for the whole room instead of committing blind to whatever a node's generated tier
    /// happened to be. Delegates entirely to <see cref="Room.SetDesiredRiskTierForPendingCombatNodes"/>.
    /// </summary>
    public void SetRoomDesiredRiskTier(RiskTier tier)
    {
        EnsureActive();

        CurrentRoom.SetDesiredRiskTierForPendingCombatNodes(tier);
    }

    /// <summary>
    /// Moves the party toward the given cell within the current grid room. Delegates entirely
    /// to <see cref="Room.MoveParty"/>.
    /// </summary>
    public sealed record GroundPickupResult(
        IReadOnlyCollection<RunItemId> CollectedItemIds,
        IReadOnlyCollection<RunItemId> BlockedItemIds);

    public GroundPickupResult MoveParty(int targetX, int targetY)
    {
        EnsureActive();

        var move = CurrentRoom.MoveParty(targetX, targetY);
        var collected = new List<RunItemId>();
        var blocked = new List<RunItemId>();

        foreach (var cell in move.TraversedCells)
        {
            foreach (var item in _runItems
                         .Where(candidate =>
                             candidate.GroundRoomId == CurrentRoom.Id.Value
                             && candidate.GroundX == cell.X
                             && candidate.GroundY == cell.Y)
                         .ToArray())
            {
                if (TryCollectGroundItem(item))
                    collected.Add(item.Id);
                else
                    blocked.Add(item.Id);
            }
        }

        return new GroundPickupResult(collected, blocked);
    }

    /// <summary>
    /// Searches the ground around the party for hidden nodes, at the cost of movement budget.
    /// Delegates to the current room — see <see cref="Room.SearchAtPartyPosition"/>.
    /// </summary>
    public void SearchAtPartyPosition()
    {
        EnsureActive();

        CurrentRoom.SearchAtPartyPosition();
    }

    /// <summary>
    /// Enters the node the party currently stands on within the current grid room. Delegates
    /// entirely to <see cref="Room.EnterNodeAtPartyPosition"/>.
    /// </summary>
    public void EnterGridNode(Guid nodeId)
    {
        EnsureActive();

        CurrentRoom.EnterNodeAtPartyPosition(new NodeId(nodeId));
    }

    /// <summary>
    /// Challenges the room boss remotely once the movement budget is exhausted, without
    /// requiring the party to walk onto its cell. Delegates entirely to
    /// <see cref="Room.ChallengeBossRemotely"/>.
    /// </summary>
    public void ChallengeBossRemotely()
    {
        EnsureActive();

        CurrentRoom.ChallengeBossRemotely();
    }

    public void ResolveCurrentEvent()
    {
        EnsureActive();

        // The room's boss no longer locks anything shut once defeated — Run stays Active
        // whichever node was just resolved (see Room.ResolveSelectedNodeEvent, Run.ConfirmRoomExit).
        CurrentRoom.ResolveSelectedNodeEvent();
    }

    public void ProgressCurrentRoom()
    {
        EnsureActive();

        CurrentRoom.ReturnToExploration();
    }

    /// <summary>
    /// Walks the party through a confirmed room exit (see NodeEventType.Exit) — "valider la
    /// sortie". No longer requires the room's boss to be defeated: it just needs the party to
    /// actually be standing on the exit node's cell, with nothing else pending. Increments
    /// <see cref="CurrentRoomIndex"/> and stays <see cref="RunStatus.Active"/> (or advances to
    /// <see cref="RunStatus.BossReached"/> for the Final room). Signals which floor-end payouts
    /// are due when this transition just crossed a floor boundary (see
    /// <see cref="ConsumeFloorEndModifiers"/>).
    /// </summary>
    public FloorEndModifierConsumptionResult ConfirmRoomExit(NodeId exitNodeId, Room nextRoom)
    {
        EnsureActive();

        if (HasActiveCombat)
        {
            throw new DomainException("Cannot confirm this exit: run has an active combat.");
        }

        if (HasPendingRewardOffer)
        {
            throw new DomainException(
                "Cannot confirm this exit: run has a pending reward offer that must be selected first.");
        }

        if (nextRoom.Depth != CurrentDepth + 1)
        {
            throw new DomainException("Next room depth must be current depth + 1.");
        }

        _roomSnapshot = CreateSnapshot();

        CurrentRoom.EnterNodeAtPartyPosition(exitNodeId);
        CurrentRoom.ResolveSelectedNodeEvent();

        var previousFloorIndex = FloorIndex;
        _runItems.RemoveAll(item =>
            item.GroundRoomId == CurrentRoom.Id.Value);

        // "Loi du Répit" (law.repit): laws suspended for the room now being left resume —
        // done before the floor-end sweep below so a resumed UntilFloorEnds modifier can
        // still be correctly consumed if this same transition also crosses a floor boundary.
        ResumeSuspendedSevereLawModifiers();

        // Run sans fin : aucune profondeur maximale. La room boss (Him'Lit) est portee
        // par son type de room, que le generateur produit tous les 10 rooms.
        _rooms.Add(nextRoom);
        CurrentRoomId = nextRoom.Id;
        CurrentRoomIndex++;
        Status = nextRoom.RoomType == RoomType.Final
            ? RunStatus.BossReached
            : RunStatus.Active;

        ApplyForcedWeatherPlanToCurrentRoom();

        return FloorIndex != previousFloorIndex
            ? ConsumeFloorEndModifiers()
            : FloorEndModifierConsumptionResult.None;
    }

    public void CompleteRun(DateTimeOffset endedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is already closed.");
        }

        Status = RunStatus.Completed;
        EndedAt = endedAt;
    }

    public void FailRun(DateTimeOffset endedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is already closed.");
        }

        Status = RunStatus.Failed;
        EndedAt = endedAt;
    }

    public void Abandon(DateTimeOffset endedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is already closed.");
        }

        Status = RunStatus.Abandoned;
        EndedAt = endedAt;
    }

    /// <summary>
    /// Suspends the run at a safe point and returns the player to the main menu. The run can
    /// be resumed later.
    /// </summary>
    /// <remarks>
    /// Safe point: nothing in progress — no boss dependency anymore (the boss no longer gates
    /// anything, see Run.ConfirmRoomExit). The run must be <see cref="RunStatus.Active"/>, with
    /// no active combat, no pending reward offer, and the current room back in free exploration
    /// (not mid node-selection/resolution).
    /// </remarks>
    public void SaveAndExit(DateTimeOffset savedAt)
    {
        EnsureActive();

        if (CurrentRoom.State != RoomState.Active)
        {
            throw new DomainException(
                "Cannot save and exit: the current room's event must be resolved first.");
        }

        if (HasActiveCombat)
        {
            throw new DomainException("Cannot save and exit: run has an active combat.");
        }

        if (HasPendingRewardOffer)
        {
            throw new DomainException(
                "Cannot save and exit: run has a pending reward offer that must be selected first.");
        }

        _preSuspendStatus = Status;
        Status = RunStatus.Suspended;
        SavedAt = savedAt;
    }

    /// <summary>
    /// Resumes the run from a suspended state, restoring the pre-suspend status
    /// so the player can continue from where they left off.
    /// </summary>
    public void Resume()
    {
        if (Status != RunStatus.Suspended)
        {
            throw new DomainException("Cannot resume a run that is not suspended.");
        }

        if (_preSuspendStatus is null)
        {
            throw new DomainException(
                "Cannot resume: pre-suspend status is missing. The run may have been saved by an older version.");
        }

        var restoredStatus = _preSuspendStatus.Value;
        Status = restoredStatus;
        SavedAt = null;
        _preSuspendStatus = null;

        // Recreate a snapshot so the player can exit the room again after resuming.
        if (restoredStatus == RunStatus.Active)
        {
            _roomSnapshot = CreateSnapshot();
        }
    }

    /// <summary>
    /// Attache le combat tactique à la run en garantissant l'unicité du combat actif.
    /// </summary>
    public void StartTacticalCombat(Combats.Tactical.TacticalCombat combat)
    {
        ArgumentNullException.ThrowIfNull(combat);

        if (combat.Id.Value == Guid.Empty)
        {
            throw new DomainException("Combat id is required.");
        }

        if (combat.RunId != Id)
        {
            throw new DomainException("Combat does not belong to this run.");
        }

        if (combat.Status != CombatStatus.Active)
        {
            throw new DomainException("Combat must be active to be started.");
        }

        if (Status != RunStatus.Active)
        {
            throw new DomainException("Run must be active to start a combat.");
        }

        if (_activeTacticalCombat is not null)
        {
            throw new DomainException("Run already has an active combat.");
        }

        if (ActiveCombatId.HasValue && ActiveCombatId.Value != combat.Id)
        {
            throw new DomainException("Combat does not match the active run combat.");
        }

        ActiveCombatId = combat.Id;
        _activeTacticalCombat = combat;
    }

    /// <summary>
    /// Le combat tactique en cours, ou une erreur explicite s'il n'y en a pas. Les commandes
    /// tactiques passent toutes par là plutôt que de déréférencer
    /// <see cref="ActiveTacticalCombat"/>.
    /// </summary>
    public Combats.Tactical.TacticalCombat RequireActiveTacticalCombat()
    {
        if (_activeTacticalCombat is null)
        {
            throw new DomainException("Run has no active tactical combat.");
        }

        return _activeTacticalCombat;
    }

    public void CompleteActiveCombat(CombatId combatId)
    {
        if (!HasActiveCombat)
        {
            throw new DomainException("Run has no active combat.");
        }

        if (ActiveCombatId != combatId)
        {
            throw new DomainException("Combat does not match the active run combat.");
        }

        ActiveCombatId = null;
        _activeTacticalCombat = null;

        ResolveCurrentEvent();
    }

    /// <summary>
    /// Clôt le combat tactique en cours sur une victoire.
    /// </summary>
    public void CompleteActiveCombat()
    {
        var status = _activeTacticalCombat?.Status;

        if (status is null)
        {
            throw new DomainException("Run has no active combat.");
        }

        if (status != CombatStatus.Completed)
        {
            throw new DomainException("Active combat must be completed before resolving combat victory.");
        }

        OnCombatWon();

        ActiveCombatId = null;
        _activeTacticalCombat = null;

        ResolveCurrentEvent();
    }

    /// <inheritdoc cref="CompleteActiveCombat"/>
    public void FailActiveCombat(DateTimeOffset endedAt)
    {
        var status = _activeTacticalCombat?.Status;

        if (status is null)
        {
            throw new DomainException("Run has no active combat.");
        }

        if (status != CombatStatus.Failed)
        {
            throw new DomainException("Active combat must be failed before resolving combat defeat.");
        }

        ActiveCombatId = null;
        _activeTacticalCombat = null;
        Status = RunStatus.Failed;
        EndedAt = endedAt;
    }

    /// <summary>
    /// Résout un combat normal par la sortie tactique. L'événement est consommé, sans victoire,
    /// sans récompense et sans déclencher les effets de fin de combat gagnée.
    /// </summary>
    public void EscapeActiveCombat()
    {
        if (_activeTacticalCombat?.Status != CombatStatus.Escaped)
            throw new DomainException("Active tactical combat must be escaped before resolving a flee.");

        ActiveCombatId = null;
        _activeTacticalCombat = null;
        PendingRewardOfferId = null;
        ResolveCurrentEvent();
    }

    public void SetPendingRewardOffer(RewardOfferId rewardOfferId)
    {
        if (rewardOfferId.Value == Guid.Empty)
        {
            throw new DomainException("Reward offer id is required.");
        }

        if (Status != RunStatus.Active)
        {
            throw new DomainException("Run must be active to set a pending reward offer.");
        }

        if (HasPendingRewardOffer)
        {
            throw new DomainException("Run already has a pending reward offer.");
        }

        PendingRewardOfferId = rewardOfferId;
    }

    public void ClearPendingRewardOffer()
    {
        if (!HasPendingRewardOffer)
        {
            throw new DomainException("Run has no pending reward offer.");
        }

        PendingRewardOfferId = null;
    }

    public void ApplyHeal(int amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Heal amount must be positive.");
        }

        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }

    /// <summary>
    /// Out-of-combat vitality loss (e.g. an NPC consequence). Clamped to a minimum of 1
    /// so a map encounter never kills the player directly.
    /// </summary>
    public void ApplyVitalityLoss(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHp = Math.Max(1, CurrentHp - amount);
        PlayerState?.LoseVitality(amount, floor: 1);
    }

    /// <summary>Percentage of <see cref="RunModifierType.RoomTraversalHpDrain"/>'s "Dévoration" drain.</summary>
    private const int RoomTraversalHpDrainPercent = 3;

    /// <summary>Percentage of max vitality "Dévoration" restores per combat WON.</summary>
    private const int DevorationCombatWinRestorePercent = 5;

    /// <summary>
    /// "Dévoration" — when the <see cref="RunModifierType.RoomTraversalHpDrain"/> law is
    /// active and the player traverses a room without resolving any combat node in it,
    /// drains <see cref="RoomTraversalHpDrainPercent"/>% of max vitality. Called by the
    /// room-transition handler once it determines the room just left had no Combat/Elite/
    /// RoomBoss resolution.
    /// </summary>
    public void OnRoomEnteredWithoutCombat()
    {
        if (!_runModifiers.Any(m => m.Type == RunModifierType.RoomTraversalHpDrain && !m.IsConsumed))
            return;

        ApplyVitalityLoss(PercentOf(MaxHp, RoomTraversalHpDrainPercent));
    }

    /// <summary>
    /// "Dévoration" — the other half of law.devoration: "chaque combat remporté
    /// restaure 5%" of max vitality, under the same <see cref="RunModifierType.RoomTraversalHpDrain"/>
    /// modifier as the room-traversal drain. Called from <see cref="CompleteActiveCombat()"/>
    /// on every combat victory.
    /// </summary>
    private void OnCombatWon()
    {
        if (!_runModifiers.Any(m => m.Type == RunModifierType.RoomTraversalHpDrain && !m.IsConsumed))
            return;

        var restoreAmount = PercentOf(MaxHp, DevorationCombatWinRestorePercent);
        if (restoreAmount > 0)
            ApplyHeal(restoreAmount);
    }

    public void ApplyStatBonus(string stat, int value)
    {
        if (string.IsNullOrWhiteSpace(stat))
        {
            throw new DomainException("Stat name is required.");
        }

        switch (stat.Trim().ToLowerInvariant())
        {
            case "attack":
                Attack += value;
                break;

            case "defense":
                Defense += value;
                break;

            case "speed":
                Speed += value;
                break;

            case "all":
                Attack += value;
                Defense += value;
                break;

            default:
                throw new DomainException($"Unknown stat: '{stat}'.");
        }
    }

    public void AddMemoryFragment(string fragmentKey)
    {
        if (string.IsNullOrWhiteSpace(fragmentKey))
        {
            throw new DomainException("Memory fragment key is required.");
        }

        _memoryFragments.Add(fragmentKey.Trim());
    }

    /// <summary>
    /// Appends a line to the run's journal (Carnet de bord), auto-tagged with the room the run
    /// is currently in. A silent no-op when <see cref="JournalEnabled"/> is false, so callers
    /// don't need to guard every call site.
    /// </summary>
    public void AppendJournalEntry(string text)
    {
        if (!JournalEnabled || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _journalEntries.Add(new RunJournalEntry(CurrentRoomIndex, CurrentRoom.CatalogBinding?.DisplayName, text.Trim()));
    }

    public void ApplyReward(RewardChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned or RunStatus.Suspended)
        {
            throw new DomainException("Closed runs cannot receive rewards.");
        }

        if (!HasPendingRewardOffer)
        {
            throw new DomainException("Run has no pending reward offer.");
        }

        switch (choice.RewardType)
        {
            case RewardType.Heal:
                var healAmount = ParsePayloadAmount(choice, "heal");
                ApplyHeal(healAmount);
                PlayerState.Heal(healAmount);
                break;

            case RewardType.TemporaryItem:
                var itemKey = choice.PayloadKey;
                AddRunItemFromPayload(itemKey);
                break;

            case RewardType.MemoryFragment:
                AddMemoryFragment(choice.PayloadKey);
                break;

            case RewardType.Decline:
                // No-op: the player walked away from the offer (e.g. a merchant's
                // "Refuser") without spending anything or receiving anything.
                break;

            default:
                throw new DomainException($"Reward type '{choice.RewardType}' is not supported.");
        }
    }

    public void ApplyRewardEffect(RewardChoice choice)
    {
        ApplyReward(choice);
    }

    // Unconditional, unlimited add — used by reward selection (ApplyReward/EnrichLastAddedItem,
    // which assumes the just-added item is always _runItems.LastOrDefault()) and NPC-granted
    // offerings. Both are curated, bounded flows (a chosen reward option, an authored offer),
    // deliberately left outside the run-bag capacity check below — only exploration pickups
    // (TryAddRunItem) enforce RunItemCapacity (SFD "Équipement et sac permanent" § 5).
    public void AddRunItem(RunItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.CollectFromGround();
        var existing = _runItems.FirstOrDefault(i =>
            i.DefinitionKey == item.DefinitionKey &&
            i.Type == item.Type &&
            !i.IsOnGround &&
            i.EffectiveMaxStack > 1);

        if (existing is not null)
        {
            existing.AddQuantity(item.Quantity);
        }
        else
        {
            _runItems.Add(item);
        }

        ApplyItemGrantModifiers(item.EffectType, item.EffectAmount, item.DefinitionKey);
    }

    /// <summary>
    /// Capacity-aware add for exploration pickups. Merging into an existing stack never
    /// counts against capacity (it isn't a new distinct entry); a genuinely new item is
    /// rejected once <see cref="RunItemCapacity"/> distinct entries are already held.
    /// </summary>
    public bool TryAddRunItem(RunItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.CollectFromGround();
        var existing = _runItems.FirstOrDefault(i =>
            i.DefinitionKey == item.DefinitionKey &&
            i.Type == item.Type &&
            !i.IsOnGround &&
            i.EffectiveMaxStack > 1);

        if (existing is not null)
        {
            if (!existing.CanAddQuantity(item.Quantity))
            {
                return false;
            }

            existing.AddQuantity(item.Quantity);
            return true;
        }

        if (RunItems.Count >= RunItemCapacity)
        {
            return false;
        }

        _runItems.Add(item);
        return true;
    }

    private bool TryCollectGroundItem(RunItem groundItem)
    {
        var stack = _runItems.FirstOrDefault(item =>
            !item.IsOnGround
            && item.DefinitionKey == groundItem.DefinitionKey
            && item.Type == groundItem.Type
            && item.EffectiveMaxStack > 1);

        if (stack is not null)
        {
            if (!stack.CanAddQuantity(groundItem.Quantity))
                return false;

            stack.AddQuantity(groundItem.Quantity);
            _runItems.Remove(groundItem);
            ApplyItemGrantModifiers(
                groundItem.EffectType,
                groundItem.EffectAmount,
                groundItem.DefinitionKey);
            return true;
        }

        if (RunItems.Count >= RunItemCapacity)
            return false;

        groundItem.CollectFromGround();
        ApplyItemGrantModifiers(
            groundItem.EffectType,
            groundItem.EffectAmount,
            groundItem.DefinitionKey);
        return true;
    }

    /// <summary>
    /// Scatters combat loot (see GroundLootBuilder — a small, secondary pool, independent from
    /// the curated reward-choice offer) onto the ground at/near the given cell, one item per
    /// distinct nearby floor cell where possible so multiple pickups don't visually stack.
    /// Collected automatically by walking over it, exactly like a player-dropped item (see
    /// MoveParty) — never capacity-checked, matching every other ground item: only converting
    /// ground to held inventory checks RunItemCapacity.
    /// </summary>
    public void DropCombatLootOnGround(IReadOnlyCollection<RunItem> items, int centerX, int centerY)
    {
        EnsureActive();

        if (items.Count == 0)
        {
            return;
        }

        var candidateCells = new (int X, int Y)[]
            {
                (centerX, centerY),
                (centerX + 1, centerY),
                (centerX - 1, centerY),
                (centerX, centerY + 1),
                (centerX, centerY - 1),
            }
            .Where(cell => CurrentRoom.Grid.IsWalkable(cell.X, cell.Y))
            .ToArray();

        // The combat's own cell is walkable by construction (nodes only ever stand on floor),
        // so this is only reached if every neighbor happens to be blocked — stack on the
        // center rather than drop nothing.
        var fallbackCell = candidateCells.Length > 0 ? candidateCells[0] : (centerX, centerY);

        var cellIndex = 0;
        foreach (var item in items)
        {
            var (x, y) = cellIndex < candidateCells.Length ? candidateCells[cellIndex] : fallbackCell;
            item.PlaceOnGround(CurrentRoom.Id.Value, x, y);
            _runItems.Add(item);
            cellIndex++;
        }
    }

    /// <summary>
    /// Resolves the explicit inventory choice made after a ground pickup was blocked.
    /// The selected held item is left on the exact loot cell and the ground item takes
    /// its inventory slot, so the operation never deletes loot or exceeds capacity.
    /// </summary>
    public void SwapGroundItemIntoInventory(RunItemId groundItemId, RunItemId heldItemId)
    {
        EnsureActive();

        if (groundItemId == heldItemId)
            throw new DomainException("Ground and held items must be different.");

        var groundItem = _runItems.FirstOrDefault(item => item.Id == groundItemId)
            ?? throw new DomainException("Ground item was not found.");
        var heldItem = _runItems.FirstOrDefault(item => item.Id == heldItemId)
            ?? throw new DomainException("Held item was not found.");

        if (!groundItem.IsOnGround || groundItem.GroundRoomId != CurrentRoom.Id.Value)
            throw new DomainException("The selected ground item is not in the current room.");
        if (heldItem.IsOnGround)
            throw new DomainException("The selected replacement is not held in the inventory.");

        var groundX = groundItem.GroundX!.Value;
        var groundY = groundItem.GroundY!.Value;

        ConsumeItemGrantModifiers(heldItem.DefinitionKey);
        heldItem.PlaceOnGround(CurrentRoom.Id.Value, groundX, groundY);
        groundItem.CollectFromGround();
        ApplyItemGrantModifiers(
            groundItem.EffectType,
            groundItem.EffectAmount,
            groundItem.DefinitionKey);
    }

    private void ConsumeItemGrantModifiers(string definitionKey)
    {
        var now = DateTime.UtcNow;
        foreach (var modifier in _runModifiers.Where(modifier =>
                     !modifier.IsConsumed
                     && string.Equals(modifier.SourceType, "RunItem", StringComparison.Ordinal)
                     && string.Equals(modifier.SourceKey, definitionKey, StringComparison.Ordinal)))
        {
            modifier.Consume(now);
        }
    }

    public void EnrichLastAddedItem(
        string definitionVersion,
        string? narrativeText,
        string category,
        string usageMode,
        string lifecycle,
        int maxStack,
        bool isUsableInCombat,
        bool isUsableOutsideCombat,
        Guid sourceRewardOptionId,
        bool isContainer = false,
        int? containerCapacity = null,
        bool isLiquid = false)
        => EnrichLastAddedItem(
            definitionVersion,
            narrativeText,
            category,
            usageMode,
            lifecycle,
            maxStack,
            isUsableInCombat,
            isUsableOutsideCombat,
            sourceRewardOptionId,
            isContainer,
            containerCapacity,
            isLiquid,
            tacticalRange: 1,
            tacticalAreaShape: "Single",
            requiresLineOfSight: false);

    public void EnrichLastAddedItem(
        string definitionVersion,
        string? narrativeText,
        string category,
        string usageMode,
        string lifecycle,
        int maxStack,
        bool isUsableInCombat,
        bool isUsableOutsideCombat,
        Guid sourceRewardOptionId,
        bool isContainer,
        int? containerCapacity,
        bool isLiquid,
        int tacticalRange,
        string tacticalAreaShape,
        bool requiresLineOfSight)
    {
        var lastItem = _runItems.LastOrDefault();
        if (lastItem is null) return;

        var enriched = RunItem.Rehydrate(
            lastItem.Id,
            lastItem.DefinitionKey,
            lastItem.DisplayName,
            lastItem.Description,
            lastItem.Type,
            lastItem.Rarity,
            lastItem.Quantity,
            lastItem.EffectType,
            lastItem.EffectAmount,
            lastItem.CreatedAtUtc,
            definitionVersion: definitionVersion,
            narrativeText: narrativeText,
            category: category,
            usageMode: usageMode,
            lifecycle: lifecycle,
            maxStack: maxStack,
            effectSummary: lastItem.EffectSummary,
            isUsableInCombat: isUsableInCombat,
            isUsableOutsideCombat: isUsableOutsideCombat,
            sourceRewardOptionId: sourceRewardOptionId,
            isContainer: isContainer,
            containerCapacity: containerCapacity,
            isLiquid: isLiquid,
            containedLiquidDefinitionKey: lastItem.ContainedLiquidDefinitionKey,
            tacticalRange: tacticalRange,
            tacticalAreaShape: tacticalAreaShape,
            requiresLineOfSight: requiresLineOfSight,
            groundRoomId: lastItem.GroundRoomId,
            groundX: lastItem.GroundX,
            groundY: lastItem.GroundY);

        var index = _runItems.FindIndex(i => i.Id == lastItem.Id);
        if (index >= 0)
        {
            _runItems[index] = enriched;
        }
    }

    /// <summary>
    /// Pours a liquid item's contents into an empty container item, consuming one unit of the
    /// liquid from the run inventory. Both items must already be in the run's inventory.
    /// </summary>
    /// <returns>Whether the poured liquid item was fully consumed (quantity reached zero).</returns>
    public bool PourLiquid(RunItemId containerId, RunItemId liquidItemId)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot use items on a closed run.");

        var container = _runItems.FirstOrDefault(i => i.Id == containerId)
            ?? throw new DomainException($"Item '{containerId.Value}' not found in run inventory.");

        var liquidItem = _runItems.FirstOrDefault(i => i.Id == liquidItemId)
            ?? throw new DomainException($"Item '{liquidItemId.Value}' not found in run inventory.");

        if (!liquidItem.IsLiquid)
            throw new DomainException($"Item '{liquidItem.DefinitionKey}' is not a liquid.");

        container.PourLiquidInto(liquidItem.DefinitionKey);
        liquidItem.ConsumeOne();

        return liquidItem.Quantity == 0;
    }

    /// <summary>
    /// Empties a container item's contents, freeing it to receive a different liquid.
    /// </summary>
    public void EmptyContainer(RunItemId containerId)
    {
        var container = _runItems.FirstOrDefault(i => i.Id == containerId)
            ?? throw new DomainException($"Item '{containerId.Value}' not found in run inventory.");

        container.EmptyContents();
    }

    /// <summary>
    /// Adds a new modifier to the run. Duplicate (unconsumed) modifiers of the same type
    /// are allowed — they stack.
    /// </summary>
    public void AddRunModifier(RunModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot add a modifier to a closed run.");

        _runModifiers.Add(modifier);
    }

    public void ForceWeather(int climateValue, int roomCount, string sourceKey)
    {
        if (HasActiveCombat)
            throw new DomainException("Weather cannot be changed during combat.");
        if (climateValue is < 5 or > 9)
            throw new DomainException("Unsupported canonical weather.");
        if (roomCount <= 0)
            throw new DomainException("Forced weather duration must be positive.");

        ConsumeActiveModifiers(RunModifierType.ForcedWeatherPlan);
        AddRunModifier(RunModifier.Create(
            RunModifierType.ForcedWeatherPlan,
            climateValue * 100 + roomCount,
            RunModifierDuration.UntilRunEnds,
            "Item",
            sourceKey,
            stackPolicy: "Replace"));
        ApplyForcedWeatherPlanToCurrentRoom();
    }

    public void RerollCurrentWeather(string sourceKey)
    {
        var current = _runModifiers
            .Where(m => m.Type == RunModifierType.RoomClimate
                && !m.IsConsumed
                && m.ExpiresAtRoomId == CurrentRoom.Id.Value)
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefault();
        var next = current?.Value switch
        {
            5 => 6,
            6 => 7,
            7 => 8,
            8 => 9,
            _ => 5
        };

        ReplaceCurrentRoomWeather(next, sourceKey);
    }

    private void ApplyForcedWeatherPlanToCurrentRoom()
    {
        var plan = _runModifiers
            .Where(m => m.Type == RunModifierType.ForcedWeatherPlan && !m.IsConsumed)
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefault();
        if (plan is null)
            return;

        var encoded = (int)Math.Round(plan.Value);
        var climate = encoded / 100;
        var remaining = encoded % 100;
        ReplaceCurrentRoomWeather(climate, plan.SourceKey);
        plan.Consume(DateTime.UtcNow);

        if (remaining > 1)
        {
            AddRunModifier(RunModifier.Create(
                RunModifierType.ForcedWeatherPlan,
                climate * 100 + remaining - 1,
                RunModifierDuration.UntilRunEnds,
                plan.SourceType,
                plan.SourceKey,
                stackPolicy: "Replace"));
        }
    }

    private void ReplaceCurrentRoomWeather(int climateValue, string sourceKey)
    {
        foreach (var modifier in _runModifiers.Where(m =>
            m.Type == RunModifierType.RoomClimate
            && !m.IsConsumed
            && m.ExpiresAtRoomId == CurrentRoom.Id.Value))
        {
            modifier.Consume(DateTime.UtcNow);
        }

        AddRunModifier(RunModifier.Create(
            RunModifierType.RoomClimate,
            climateValue,
            RunModifierDuration.UntilRoomEnds,
            "Item",
            sourceKey,
            stackPolicy: "Replace",
            expiresAtRoomId: CurrentRoom.Id.Value));
    }

    private void ConsumeActiveModifiers(RunModifierType type)
    {
        var now = DateTime.UtcNow;
        foreach (var modifier in _runModifiers.Where(m => m.Type == type && !m.IsConsumed))
            modifier.Consume(now);
    }

    /// <summary>
    /// Returns all unconsumed modifiers of the specified type.
    /// </summary>
    public IReadOnlyCollection<RunModifier> GetActiveModifiers(RunModifierType type)
        => _runModifiers
            .Where(m => m.Type == type && !m.IsConsumed)
            .ToArray();

    /// <summary>
    /// Consumes all unconsumed modifiers whose duration is <see cref="RunModifierDuration.NextCombatOnly"/>.
    /// Call this after a combat resolves.
    /// </summary>
    public void ConsumeNextCombatModifiers()
    {
        var now = DateTime.UtcNow;

        foreach (var modifier in _runModifiers
            .Where(m => m.Duration == RunModifierDuration.NextCombatOnly && !m.IsConsumed))
        {
            modifier.Consume(now);
        }
    }

    /// <summary>"Loi de l'Oubli Partiel" floor-end payout: the number of skill points
    /// the team learns from having forgotten a skill for the floor.</summary>
    public const int SkillForgottenFloorEndStatPoints = 8;

    /// <summary>"Loi du Prêteur" floor-end clawback: the fraction of the player's total
    /// currency the Palais reclaims (SFD: "25% du total détenu — intérêts compris").</summary>
    public const double PreteurClawbackFraction = 0.25;

    /// <summary>
    /// Consumes all unconsumed modifiers whose duration is <see cref="RunModifierDuration.UntilFloorEnds"/>.
    /// Called automatically from <see cref="MoveToNextRoom"/> whenever <see cref="FloorIndex"/> changes.
    /// Signals which payouts are due — the caller (an Application-layer handler) must
    /// then perform them via the player-profile gateway, since that I/O is a
    /// gateway/Application concern the Domain layer cannot reach.
    /// </summary>
    public FloorEndModifierConsumptionResult ConsumeFloorEndModifiers()
    {
        var now = DateTime.UtcNow;
        var oubliPartielPayoutDue = false;
        var preteurClawbackDue = false;

        foreach (var modifier in _runModifiers
            .Where(m => m.Duration == RunModifierDuration.UntilFloorEnds && !m.IsConsumed))
        {
            if (modifier.Type == RunModifierType.SkillForgotten)
            {
                oubliPartielPayoutDue = true;
                ForgottenSkillKey = null;
            }
            else if (modifier.Type == RunModifierType.CurrencyGainBonusPercent)
            {
                // "Loi du Prêteur" (law.preteur): the currency-gain-bonus modifier
                // doubles as this law's "active" marker — its floor-end consumption
                // signals that the 25% clawback is due.
                preteurClawbackDue = true;
            }

            modifier.Consume(now);
        }

        return new FloorEndModifierConsumptionResult(oubliPartielPayoutDue, preteurClawbackDue);
    }

    private void AddRunItemFromPayload(string payloadKey)
    {
        // Payload format: "item:<definitionKey>:<displayName>:<description>:<type>:<rarity>:<effectType>:<effectAmount>"
        var parts = payloadKey.Split(':', StringSplitOptions.TrimEntries);

        if (parts.Length < 8 || !string.Equals(parts[0], "item", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Invalid item reward payload format.");
        }

        var definitionKey = parts[1];
        var displayName = parts[2];
        var description = parts[3];
        var itemType = Enum.Parse<RunItemType>(parts[4], ignoreCase: true);
        var rarity = Enum.Parse<RunItemRarity>(parts[5], ignoreCase: true);
        var effectType = Enum.Parse<RunItemEffectType>(parts[6], ignoreCase: true);
        var effectAmount = int.Parse(parts[7]);

        var item = RunItem.Create(
            definitionKey,
            displayName,
            description,
            itemType,
            rarity,
            1,
            effectType,
            effectAmount);

        // Straight into the inventory, not onto the ground: this item is the reward the
        // player already chose from a curated offer, not something to walk over and collect.
        // AddRunItem also applies the grant modifiers (Guard bonus, attack-type override, …)
        // immediately, matching every other reward-selection path.
        AddRunItem(item);
    }

    /// <summary>
    /// Translates certain RunItemEffectTypes into a permanent (until-run-ends)
    /// RunModifier at the moment the item is granted — whether selected as a reward
    /// (<see cref="AddRunItemFromPayload"/>) or given by an NPC (<see cref="AddRunItem"/>).
    /// </summary>
    private void ApplyItemGrantModifiers(RunItemEffectType effectType, int effectAmount, string definitionKey)
    {
        // Guard items create a permanent run-scoped StartingGuardBonus modifier.
        // The bonus stacks across multiple guard items but is capped at MaxStartingGuardBonus.
        if (effectType == RunItemEffectType.Guard && effectAmount > 0)
        {
            const int maxStartingGuardBonus = 30;
            var currentGuardBonus = _runModifiers
                .Where(m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed)
                .Sum(m => (int)m.Value);

            var cappedAmount = Math.Min(effectAmount, maxStartingGuardBonus - currentGuardBonus);
            if (cappedAmount > 0)
            {
                AddRunModifier(RunModifier.Create(
                    RunModifierType.StartingGuardBonus,
                    cappedAmount,
                    RunModifierDuration.UntilRunEnds,
                    "RunItem",
                    definitionKey));
            }
        }

        // Attack-type items override the hero's emotional attack type for the rest
        // of the run (effectAmount = int value of EmotionalType). Latest wins:
        // CombatFactory reads the most recent unconsumed override at combat creation.
        if (effectType == RunItemEffectType.AttackTypeOverride && effectAmount > 0)
        {
            AddRunModifier(RunModifier.Create(
                RunModifierType.AttackTypeOverride,
                effectAmount,
                RunModifierDuration.UntilRunEnds,
                "RunItem",
                definitionKey));
        }

        // Team-wide Speed bonus for as long as the item is held this run (e.g. Rêve
        // d'Erina: +5% team Speed). effectAmount is a whole percentage; stacks
        // additively with any other SpeedBonus modifier.
        if (effectType == RunItemEffectType.TeamSpeedBonus && effectAmount > 0)
        {
            AddRunModifier(RunModifier.Create(
                RunModifierType.SpeedBonus,
                effectAmount / 100.0,
                RunModifierDuration.UntilRunEnds,
                "RunItem",
                definitionKey));
        }
    }

    /// <summary>
    /// Applies a curse to the run, replacing any existing curse.
    /// </summary>
    public void ApplyCurse(ActiveCurse curse)
    {
        ArgumentNullException.ThrowIfNull(curse);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot apply a curse to a closed run.");

        _activeCurse = curse;
    }

    /// <summary>
    /// Clears the active curse after it has been consumed.
    /// </summary>
    public void ClearActiveCurse()
    {
        _activeCurse = null;
    }

    private static int ParsePayloadAmount(RewardChoice choice, string expectedPrefix)
    {
        var parts = choice.PayloadKey.Split(':', StringSplitOptions.TrimEntries);

        if (parts.Length != 2 ||
            !string.Equals(parts[0], expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(parts[1], out var amount))
        {
            throw new DomainException($"Invalid payload for reward type '{choice.RewardType}'.");
        }

        return amount;
    }

    private RunSnapshot CreateSnapshot() => new(
        CurrentHp,
        Attack,
        Defense,
        Speed,
        [.. _memoryFragments],
        [.. _activePalaceLaws],
        [.. _runItems.Select(i => i.Id.Value)],
        [.. _runModifiers.Select(m => m.Id.Value)]);

    public CombatantSnapshot CreatePlayerSnapshot()
    {
        return CombatantSnapshot.Create(
            CombatantId.New(),
            "player-runtime-v1",
            "Player",
            CombatantSide.Player,
            maxHealth: MaxHp,
            currentHealth: CurrentHp,
            attack: Attack,
            defense: Defense,
            speed: Speed);
    }

    private void EnsureActive()
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
        {
            throw new DomainException("Run is closed.");
        }

        if (Status == RunStatus.Suspended)
        {
            throw new DomainException("Run is suspended and cannot accept game actions until resumed.");
        }

        if (Status != RunStatus.Active)
        {
            throw new DomainException("Run must be active.");
        }
    }

    /// <summary>
    /// Exits the current room, rolling back all resources (HP, stats, memory
    /// fragments, active palace laws) to the state they were in when the room
    /// was first entered. The room is reset and the run is suspended so the
    /// player can resume later from the beginning of this room.
    /// </summary>
    public void ExitMidRoom(DateTimeOffset savedAt)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned or RunStatus.Suspended)
        {
            throw new DomainException("Run is closed.");
        }

        if (Status != RunStatus.Active)
        {
            throw new DomainException("Cannot exit mid-room: run must be active.");
        }

        if (HasActiveCombat)
        {
            throw new DomainException("Cannot exit mid-room: run has an active combat.");
        }

        if (HasPendingRewardOffer)
        {
            throw new DomainException("Cannot exit mid-room: run has a pending reward offer that must be selected first.");
        }

        if (_roomSnapshot is null)
        {
            throw new DomainException("Cannot exit mid-room: no room entry snapshot available.");
        }

        var snapshot = _roomSnapshot;
        _roomSnapshot = null;

        CurrentHp = snapshot.CurrentHp;
        Attack = snapshot.Attack;
        Defense = snapshot.Defense;
        Speed = snapshot.Speed;

        _memoryFragments.Clear();
        _memoryFragments.AddRange(snapshot.MemoryFragments);

        _activePalaceLaws.Clear();
        _activePalaceLaws.AddRange(snapshot.ActivePalaceLaws);

        // "Loi du Répit" (law.repit) can only have suspended laws within the room being
        // rolled back (its own duration is exactly one room) — undo that suspension too,
        // since the snapshot predates it and the severe modifiers below must come back
        // exactly as they were at room entry, not merely present-but-still-consumed.
        ResumeSuspendedSevereLawModifiers();

        var snapshotItemIds = snapshot.RunItemIds.ToHashSet();
        _runItems.RemoveAll(item => !snapshotItemIds.Contains(item.Id.Value));

        var snapshotModifierIds = snapshot.RunModifierIds.ToHashSet();
        _runModifiers.RemoveAll(mod => !snapshotModifierIds.Contains(mod.Id.Value));

        CurrentRoom.ResetProgress();

        _preSuspendStatus = RunStatus.Active;
        Status = RunStatus.Suspended;
        SavedAt = savedAt;
    }

    public void ActivatePalaceLaw(PalaceLaw law)
    {
        ArgumentNullException.ThrowIfNull(law);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot activate a palace law on a closed run.");

        // Idempotent — same law key cannot be active twice.
        if (_activePalaceLaws.Any(activeLaw => activeLaw.Key == law.Key))
            return;

        if (law.Effects.Any(effect => effect.ModifierType == RunModifierType.RoomClimate))
        {
            ReplaceActiveRoomClimateLaws();
        }

        if (law.Effects.Any(effect => effect.ModifierType == RunModifierType.SkillForgotten))
        {
            PickForgottenSkill();
        }

        _activePalaceLaws.Add(ActivePalaceLaw.From(law));

        // Apply each mechanical effect of the law as a RunModifier.
        foreach (var effect in law.Effects)
        {
            var expiresAtRoomId = effect.Duration == RunModifierDuration.UntilRoomEnds
                ? CurrentRoomId.Value
                : (Guid?)null;

            AddRunModifier(RunModifier.Create(
                effect.ModifierType,
                effect.Value,
                effect.Duration,
                sourceType: "PalaceLaw",
                sourceKey: law.Key,
                expiresAtRoomId: expiresAtRoomId));
        }

        if (law.Effects.Any(effect => effect.ModifierType == RunModifierType.SuspendSevereLaws))
        {
            SuspendActiveSevereLawModifiers();
        }
    }

    /// <summary>
    /// "Loi du Répit" (law.repit) — ACCALMIE: pauses every currently-active Sévère-polarity
    /// law's effects for the room by marking their unconsumed modifiers as consumed. Every
    /// consumption site across the engine already filters on RunModifier.IsConsumed, so
    /// pausing them this way needs no further wiring anywhere else. Reversed by
    /// <see cref="ResumeSuspendedSevereLawModifiers"/> once the room is left. Only laws active
    /// at the moment of promulgation are captured — matches the SFD (ambient promulgation
    /// only fires once per room entry, so no new Sévère law can appear mid-room under normal
    /// play).
    /// </summary>
    private void SuspendActiveSevereLawModifiers()
    {
        var now = DateTime.UtcNow;
        var severeLawKeys = _activePalaceLaws
            .Where(law => law.Polarity == PalaceLawPolarity.Severe)
            .Select(law => law.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (severeLawKeys.Count == 0)
        {
            return;
        }

        foreach (var modifier in _runModifiers.Where(modifier =>
            modifier.SourceType == "PalaceLaw" &&
            severeLawKeys.Contains(modifier.SourceKey) &&
            !modifier.IsConsumed))
        {
            modifier.Consume(now);
            _suspendedSevereLawModifierIds.Add(modifier.Id.Value);
        }
    }

    /// <summary>
    /// Reverses <see cref="SuspendActiveSevereLawModifiers"/> — called from
    /// <see cref="MoveToNextRoom"/> (leaving the room "Loi du Répit" suspended laws in) and
    /// <see cref="ExitMidRoom"/> (rolling back to before the suspension happened). A no-op
    /// once nothing is suspended.
    /// </summary>
    private void ResumeSuspendedSevereLawModifiers()
    {
        if (_suspendedSevereLawModifierIds.Count == 0)
        {
            return;
        }

        var suspendedIds = _suspendedSevereLawModifierIds.ToHashSet();
        foreach (var modifier in _runModifiers.Where(modifier => suspendedIds.Contains(modifier.Id.Value)))
        {
            modifier.Unconsume();
        }

        _suspendedSevereLawModifierIds.Clear();
    }

    private void ReplaceActiveRoomClimateLaws()
    {
        var replacedLawKeys = _runModifiers
            .Where(modifier =>
                modifier.Type == RunModifierType.RoomClimate &&
                modifier.SourceType == "PalaceLaw" &&
                !modifier.IsConsumed)
            .Select(modifier => modifier.SourceKey)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (replacedLawKeys.Length == 0)
            return;

        var replacedLawKeySet = replacedLawKeys.ToHashSet(StringComparer.Ordinal);
        var now = DateTime.UtcNow;

        foreach (var modifier in _runModifiers.Where(modifier =>
            modifier.SourceType == "PalaceLaw" &&
            replacedLawKeySet.Contains(modifier.SourceKey) &&
            !modifier.IsConsumed))
        {
            modifier.Consume(now);
        }

        _activePalaceLaws.RemoveAll(activeLaw => replacedLawKeySet.Contains(activeLaw.Key));
    }

    /// <summary>Basic attack skill key, excluded from "Loi de l'Oubli Partiel"'s draw
    /// per the SFD ("hors Frappe") — mirrors CombatSkillActionValidator's own constant.</summary>
    private const string BasicAttackSkillKey = "skill.basic.strike";

    /// <summary>
    /// "Loi de l'Oubli Partiel" (law.oubli-partiel): picks one random skill key from the
    /// team's full roster (any character, any skill except the basic attack) to become
    /// unusable for the rest of the floor. Deterministic from the run's seed. A no-op
    /// (leaves ForgottenSkillKey unset) when no eligible skill exists yet — e.g. a solo
    /// run whose only skill so far is the basic attack.
    /// </summary>
    private void PickForgottenSkill()
    {
        var eligible = (PlayerSnapshot?.Characters ?? [])
            .SelectMany(character => character.Skills)
            .Select(skill => skill.SkillDefinitionKey)
            .Where(key => !string.Equals(key, BasicAttackSkillKey, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        if (eligible.Length == 0)
            return;

        var sample = DeterministicSampler.NextUnitInterval(Seed, Id.Value, CurrentRoomId.Value, FloorIndex);
        var index = (int)(sample * eligible.Length);
        if (index >= eligible.Length)
            index = eligible.Length - 1;

        ForgottenSkillKey = eligible[index];
    }

    /// <summary>"Loi de l'Impôt du Seuil" insolvency penalty: -2% team max HP for the
    /// rest of the floor, per unpaid room toll (cumulable — see
    /// RunModifierType.MaxHpReductionPercent).</summary>
    public const int RoomTollInsolvencyMaxHpReductionPercent = 2;

    /// <summary>
    /// "Loi de l'Impôt du Seuil" (law.impot-seuil): applied by the Application-layer
    /// handler when a room-toll payment fails (see MoveToNextRoomCommandHandler /
    /// IPlayerProfileGateway.TrySpendCurrencyAsync). Stacks across unpaid rooms within
    /// the same floor; cleared like any other UntilFloorEnds modifier at floor end.
    /// </summary>
    public void ApplyRoomTollInsolvencyDebuff()
    {
        AddRunModifier(RunModifier.Create(
            RunModifierType.MaxHpReductionPercent,
            RoomTollInsolvencyMaxHpReductionPercent,
            RunModifierDuration.UntilFloorEnds,
            sourceType: "PalaceLaw",
            sourceKey: "law.impot-seuil"));
    }

    /// <summary>
    /// "Loi de la Chandelle" (law.chandelle): consumes one free item-node reroll charge,
    /// if any is available. Unlike most UntilFloorEnds modifiers (swept in bulk at floor
    /// end), a charge is consumed one at a time, on demand, by
    /// RerollItemRewardOfferCommandHandler. Returns false when no charge remains.
    /// </summary>
    public bool TryConsumeItemNodeRerollCharge()
    {
        var charge = _runModifiers.FirstOrDefault(m =>
            m.Type == RunModifierType.ItemNodeRerollCharge && !m.IsConsumed);

        if (charge is null)
        {
            return false;
        }

        charge.Consume(DateTime.UtcNow);
        return true;
    }

    /// <summary>Number of "Loi de la Chandelle" reroll charges already consumed this
    /// floor — used as the deterministic nonce so consecutive rerolls of the same item
    /// node draw different results (see RewardOfferFactory.CreateItemRewardChoices).</summary>
    public int ConsumedItemNodeRerollCount =>
        _runModifiers.Count(m => m.Type == RunModifierType.ItemNodeRerollCharge && m.IsConsumed);

    /// <summary>
    /// Single entry point for ambient law promulgation (irréfusabilité — Compendium des Lois
    /// du Palais). Applies the Chapitre VIII majeure exclusivity (at most one active) and the
    /// explicit exclusion-pairs rule, then delegates to <see cref="ActivatePalaceLaw"/> and
    /// <see cref="EnforceCumulCap"/>. The Soupape rule (<see cref="ShouldForceCompliantPromulgation"/>)
    /// is the CALLER's responsibility when drawing which law to pass in — this method receives an
    /// already-drawn law and only rejects, it never re-rolls.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the law is now active (including if it already was — idempotent),
    /// <c>false</c> if rejected by the majeure-exclusivity or exclusion-pairs rule, in which
    /// case the caller may draw a different law instead.
    /// </returns>
    public bool PromulgateLaw(PalaceLaw law)
    {
        ArgumentNullException.ThrowIfNull(law);

        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot promulgate a palace law on a closed run.");

        if (_activePalaceLaws.Any(activeLaw => activeLaw.Key == law.Key))
            return true;

        if (law.IsMajeure && _activePalaceLaws.Any(activeLaw => activeLaw.IsMajeure))
            return false;

        if (law.ExclusionKeys.Count > 0 &&
            _activePalaceLaws.Any(activeLaw => law.ExclusionKeys.Contains(activeLaw.Key)))
        {
            return false;
        }

        ActivatePalaceLaw(law);
        LastPromulgationFloorIndex = FloorIndex;
        EnforceCumulCap();

        return true;
    }

    /// <summary>
    /// Enforces the cumul cap: at most <c>1 + profondeur/2</c> (capped at 6) active laws,
    /// where "profondeur" is <see cref="CurrentRoomIndex"/>. Revokes the oldest active law
    /// (by <see cref="ActivePalaceLaw.AppliedAtUtc"/>) — excluding <see cref="ActivePalaceLaw.IsCumulExempt"/>
    /// (Chapitre IX room-linked) laws, which count toward neither the cap nor its enforcement —
    /// until the cap is respected.
    /// </summary>
    public void EnforceCumulCap()
    {
        var cap = Math.Min(6, 1 + CurrentRoomIndex / 2);
        var now = DateTime.UtcNow;

        while (_activePalaceLaws.Count(law => !law.IsCumulExempt) > cap)
        {
            var oldest = _activePalaceLaws
                .Where(law => !law.IsCumulExempt)
                .OrderBy(law => law.AppliedAtUtc)
                .First();

            foreach (var modifier in _runModifiers.Where(modifier =>
                modifier.SourceType == "PalaceLaw" &&
                modifier.SourceKey == oldest.Key &&
                !modifier.IsConsumed))
            {
                modifier.Consume(now);
            }

            _activePalaceLaws.Remove(oldest);
        }
    }

    /// <summary>
    /// Revokes one currently active Palace Law using "Déni permanent" — the player-triggered
    /// counterpart to <see cref="ReplaceActiveRoomClimateLaws"/>'s automatic replacement. Consumes
    /// every unconsumed <see cref="RunModifier"/> the law applied and removes it from
    /// <see cref="ActivePalaceLaws"/>, then starts the cooldown (<see cref="LawDenialCooldownRooms"/>).
    /// </summary>
    public void RemovePalaceLaw(string lawKey)
    {
        if (string.IsNullOrWhiteSpace(lawKey))
        {
            throw new DomainException("Law key is required.");
        }

        if (!LawDenialEnabled)
        {
            throw new DomainException("This run has no law-denial capability.");
        }

        if (!CanUseLawDenial)
        {
            throw new DomainException("Law denial is still on cooldown.");
        }

        if (!_activePalaceLaws.Any(law => law.Key == lawKey))
        {
            throw new DomainException($"No active palace law with key '{lawKey}'.");
        }

        var now = DateTime.UtcNow;

        foreach (var modifier in _runModifiers.Where(modifier =>
            modifier.SourceType == "PalaceLaw" &&
            modifier.SourceKey == lawKey &&
            !modifier.IsConsumed))
        {
            modifier.Consume(now);
        }

        _activePalaceLaws.RemoveAll(law => law.Key == lawKey);
        LawDenialLastUsedRoomIndex = CurrentRoomIndex;
    }

    /// <summary>
    /// Uses "Calice infini" — restores 50% of the target's max vitality, once per room
    /// (<see cref="CaliceInfiniCooldownRooms"/>). When a combat is active and a target
    /// combatant is given, heals that combatant (ally or protagonist) using its own
    /// <see cref="Combatant.EffectiveHealingBonusPercent"/>, mirroring item-based combat
    /// healing (e.g. "La tasse de thé"). Otherwise heals <see cref="PlayerState"/> out of
    /// combat using the run's <see cref="HealingBonusPercent"/>.
    /// </summary>
    public void UseCaliceInfini(Guid? targetCombatantId)
    {
        if (!CaliceInfiniEnabled)
        {
            throw new DomainException("This run has no Calice infini capability.");
        }

        if (!CanUseCaliceInfini)
        {
            throw new DomainException("Calice infini is still on cooldown.");
        }

        if (_activeTacticalCombat is not null && targetCombatantId is not null)
        {
            var target = _activeTacticalCombat.Allies
                .FirstOrDefault(a => a.Id.Value == targetCombatantId.Value);

            if (target is null)
            {
                throw new DomainException($"No ally combatant with id '{targetCombatantId.Value}'.");
            }

            if (!target.IsDefeated)
            {
                target.ApplyHeal(ApplyHealingBonus(
                    PercentOf(target.MaxVitality, 50),
                    target.EffectiveHealingBonusPercent));
            }
        }
        else
        {
            var healAmount = ApplyHealingBonus(PercentOf(PlayerState.MaxVitality, 50), HealingBonusPercent);
            if (healAmount > 0)
            {
                PlayerState.Heal(healAmount);
                CurrentHp = PlayerState.CurrentVitality;
            }
        }

        CaliceInfiniLastUsedRoomIndex = CurrentRoomIndex;
    }

    public void DebugClearActivePalaceLaws()
    {
        var now = DateTime.UtcNow;

        foreach (var modifier in _runModifiers.Where(modifier =>
            modifier.SourceType == "PalaceLaw" &&
            !modifier.IsConsumed))
        {
            modifier.Consume(now);
        }

        _activePalaceLaws.Clear();
    }

    public void DebugClearActiveCurse()
    {
        var now = DateTime.UtcNow;

        foreach (var modifier in _runModifiers.Where(modifier =>
            modifier.SourceType == "Curse" &&
            !modifier.IsConsumed))
        {
            modifier.Consume(now);
        }

        _activeCurse = null;
    }

    /// <summary>
    /// Uses a consumable item from the run inventory.
    /// Applies its effect to <see cref="PlayerState"/> and, if a combat is active,
    /// to the player combatant inside that combat.
    /// </summary>
    /// <returns>
    /// A tuple describing what was applied: the effect type, the amount, and whether
    /// the item was fully consumed (quantity reached zero).
    /// </returns>
    /// <summary>"Loi des Poches Cousues" (law.poches-cousues) out-of-combat consumable bonus, in percent.</summary>
    private const int ConsumablesRestrictedBonusPercent = 25;

    public (RunItemEffectType effectType, int amount, bool itemDepleted) UseItem(RunItemId itemId)
    {
        if (Status is RunStatus.Completed or RunStatus.Failed or RunStatus.Abandoned)
            throw new DomainException("Cannot use items on a closed run.");
        if (HasActiveCombat)
            throw new DomainException(
                "During tactical combat, items must be used through the tactical targeting action.");

        var item = _runItems.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Item '{itemId.Value}' not found in run inventory.");

        var consumablesRestricted = _runModifiers
            .Any(m => m.Type == RunModifierType.ConsumablesRestrictedInCombat && !m.IsConsumed);

        // ConsumeOne validates a consumable-like type and a positive quantity.
        item.ConsumeOne();

        // Bonus only applies out of combat (the "in combat" branch above already threw
        // Reaching this branch means the item is used outside combat.
        var effectAmount = consumablesRestricted
            ? (int)Math.Round(item.EffectAmount * (1.0 + ConsumablesRestrictedBonusPercent / 100.0), MidpointRounding.AwayFromZero)
            : item.EffectAmount;

        ApplyItemEffectToPlayerState(item, effectAmount);

        var depleted = item.Quantity == 0;
        return (item.EffectType, effectAmount, depleted);
    }

    private void ApplyItemEffectToPlayerState(RunItem item, int effectAmount)
    {
        switch (item.EffectType)
        {
            case RunItemEffectType.Heal:
                PlayerState.Heal(ApplyHealingBonus(effectAmount, HealingBonusPercent));
                CurrentHp = PlayerState.CurrentVitality;
                break;

            case RunItemEffectType.Guard:
                PlayerState.GainGuard(effectAmount);
                break;

            case RunItemEffectType.ManaRestore:
                PlayerState.GainMana(effectAmount);
                break;

            case RunItemEffectType.ChargeRestore:
                PlayerState.GainCharge(effectAmount);
                break;

            case RunItemEffectType.HealAndManaRestorePercent:
                var healAmount = ApplyHealingBonus(PercentOf(PlayerState.MaxVitality, effectAmount), HealingBonusPercent);
                if (healAmount > 0)
                {
                    PlayerState.Heal(healAmount);
                    CurrentHp = PlayerState.CurrentVitality;
                }

                var manaAmount = PercentOf(PlayerState.MaxMana, effectAmount);
                if (manaAmount > 0)
                {
                    PlayerState.GainMana(manaAmount);
                }
                break;

            // These four are combat-only in their secondary effect (dispelling a DoT stack,
            // applying Silence/Evasion) — those statuses don't exist outside combat, so only
            // the shared heal-percent component applies here. Mirrors UseTacticalItemCommandHandler's
            // ApplyEffect, minus the combat-only status application.
            case RunItemEffectType.HealPercent:
            case RunItemEffectType.ConditionalHealOrPoison:
            case RunItemEffectType.HealPercentAndCleanseDot:
            case RunItemEffectType.HealPercentAndSilence:
            case RunItemEffectType.HealPercentAndEvasion:
                var healPercentAmount = ApplyHealingBonus(PercentOf(PlayerState.MaxVitality, effectAmount), HealingBonusPercent);
                if (healPercentAmount > 0)
                {
                    PlayerState.Heal(healPercentAmount);
                    CurrentHp = PlayerState.CurrentVitality;
                }
                break;

            case RunItemEffectType.RevivePercent:
                // Requires a defeated ally to target — meaningless outside a tactical
                // combat's targeting action (see UseTacticalItemCommandHandler).
                throw new DomainException(
                    $"Item effect '{item.EffectType}' can only be used in combat, targeting a defeated ally.");

            case RunItemEffectType.ForceWeatherOrage:
                ForceWeather(6, Math.Max(1, effectAmount), item.DefinitionKey);
                break;

            case RunItemEffectType.ForceWeatherAccalmie:
                ForceWeather(9, 1, item.DefinitionKey);
                break;

            case RunItemEffectType.RerollWeather:
                RerollCurrentWeather(item.DefinitionKey);
                break;

            case RunItemEffectType.GrantTeamSkillPoints:
                // Persistent progression is credited by the application handler.
                break;

            case RunItemEffectType.None:
            case RunItemEffectType.NextCombatGuard:
            case RunItemEffectType.NarrativeFragment:
            case RunItemEffectType.GrantTemporarySkill:
                // These effect types are not manually activatable from inventory.
                throw new DomainException(
                    $"Item effect '{item.EffectType}' cannot be triggered manually from inventory.");

            default:
                throw new DomainException(
                    $"Unsupported item effect type: '{item.EffectType}'.");
        }
    }

    /// <summary>Rounds <paramref name="value"/> * <paramref name="percent"/>% to the nearest int.</summary>
    private static int PercentOf(int value, int percent) => (int)Math.Round(value * (percent / 100.0));

    /// <summary>
    /// Scales a heal amount by an equipment-driven healing bonus (e.g. La tasse du
    /// majordome: +15%) — the run's own <see cref="HealingBonusPercent"/> for
    /// out-of-combat heals, or a specific combatant's <c>EffectiveHealingBonusPercent</c>.
    /// </summary>
    private static int ApplyHealingBonus(int amount, int healingBonusPercent)
        => (int)Math.Round(amount * (1.0 + healingBonusPercent / 100.0));

    // -----------------------------------------------------------------------
    // Rehydration (persistence restore)
    // -----------------------------------------------------------------------

    public RunSnapshotData? SnapshotData => _roomSnapshot is null
        ? null
        : new RunSnapshotData(
            _roomSnapshot.CurrentHp,
            _roomSnapshot.Attack,
            _roomSnapshot.Defense,
            _roomSnapshot.Speed,
            _roomSnapshot.MemoryFragments,
            _roomSnapshot.ActivePalaceLaws,
            _roomSnapshot.RunItemIds,
            _roomSnapshot.RunModifierIds);

    public RunStatus? PreSuspendStatus => _preSuspendStatus;

    public static Run Rehydrate(
        RunId id,
        Guid playerId,
        string seed,
        string generatorVersion,
        string markovMatrixVersion,
        RunStatus status,
        RoomId currentRoomId,
        CombatId? activeCombatId,
        RewardOfferId? pendingRewardOfferId,
        int maxHp,
        int currentHp,
        int attack,
        int defense,
        int speed,
        int focus,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt,
        DateTimeOffset? savedAt,
        int currentRoomIndex,
        IEnumerable<Room> rooms,
        IEnumerable<string> memoryFragments,
        IEnumerable<ActivePalaceLaw> activePalaceLaws,
        RunStatus? preSuspendStatus,
        RunSnapshotData? snapshot,
        PlayerRuntimeState? playerState = null,
        IEnumerable<RunItem>? runItems = null,
        IEnumerable<RunModifier>? runModifiers = null,
        RunPlayerSnapshot? playerSnapshot = null,
        ActiveCurse? activeCurse = null,
        int runItemCapacity = DefaultRunItemCapacity,
        IReadOnlyDictionary<string, int>? typedDamageReductions = null,
        int hitChanceBonusPercent = 0,
        int dotDurationReductionPercent = 0,
        int dotDamageReductionPercent = 0,
        int magicDamageBonusPercent = 0,
        int magicDamageReductionPercent = 0,
        int criticalChanceBonusPercent = 0,
        int guardBonusPercent = 0,
        int dotDamageBonusPercent = 0,
        bool journalEnabled = false,
        IEnumerable<RunJournalEntry>? journalEntries = null,
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
        IEnumerable<Guid>? suspendedSevereLawModifierIds = null,
        Combats.Tactical.TacticalCombat? activeTacticalCombat = null,
        EmotionalAffinityMatrixSnapshot? emotionalAffinityMatrix = null)
    {
        var firstRoom = rooms.First();

        var run = new Run(id, playerId, seed, generatorVersion, markovMatrixVersion, status, firstRoom, startedAt, maxHp, currentHp, attack, defense, speed, focus, currentRoomIndex, activeCombatId, pendingRewardOfferId, runItemCapacity, typedDamageReductions, hitChanceBonusPercent, dotDurationReductionPercent, dotDamageReductionPercent, dotDamageBonusPercent, magicDamageBonusPercent, magicDamageReductionPercent, criticalChanceBonusPercent, guardBonusPercent, journalEnabled, lawDenialEnabled, lawDenialLastUsedRoomIndex, reputationGainBonusPercent, himLitProtectionEnabled, healingBonusPercent, caliceInfiniEnabled, caliceInfiniLastUsedRoomIndex, magicAttack, magicDefense, lastPromulgationFloorIndex, forgottenSkillKey, emotionalAffinityMatrix);
        foreach (var room in rooms.Skip(1))
        {
            run._rooms.Add(room);
        }

        run.CurrentRoomId = currentRoomId;
        run.EndedAt = endedAt;
        run.SavedAt = savedAt;
        run._preSuspendStatus = preSuspendStatus;
        run._memoryFragments.AddRange(memoryFragments);
        run._journalEntries.AddRange(journalEntries ?? []);
        run._activePalaceLaws.AddRange(activePalaceLaws);
        run._activeCurse = activeCurse;

        if (snapshot is not null)
        {
            run._roomSnapshot = new RunSnapshot(
                snapshot.CurrentHp,
                snapshot.Attack,
                snapshot.Defense,
                snapshot.Speed,
                snapshot.MemoryFragments,
                snapshot.ActivePalaceLaws,
                snapshot.RunItemIds ?? [],
                snapshot.RunModifierIds ?? []);
        }

        run._activeTacticalCombat = activeTacticalCombat;

        if (runItems is not null)
        {
            run._runItems.AddRange(runItems);
        }

        if (runModifiers is not null)
        {
            run._runModifiers.AddRange(runModifiers);
        }

        if (suspendedSevereLawModifierIds is not null)
        {
            run._suspendedSevereLawModifierIds.AddRange(suspendedSevereLawModifierIds);
        }

        run._playerSnapshot = playerSnapshot;

        run.PlayerState = playerState ?? PlayerRuntimeState.Create(
            maxVitality: maxHp,
            skills: CreateDefaultPlayerSkills(),
            currentVitality: currentHp);

        return run;
    }

    public sealed record RunSnapshotData(
        int CurrentHp,
        int Attack,
        int Defense,
        int Speed,
        string[] MemoryFragments,
        ActivePalaceLaw[] ActivePalaceLaws,
        Guid[]? RunItemIds = null,
        Guid[]? RunModifierIds = null);

    /// <summary>
    /// Signals which floor-end payouts an Application-layer handler must perform via the
    /// player-profile gateway after a room transition crosses a floor boundary. See
    /// <see cref="ConsumeFloorEndModifiers"/>/<see cref="MoveToNextRoom"/>.
    /// </summary>
    public readonly record struct FloorEndModifierConsumptionResult(
        bool OubliPartielPayoutDue,
        bool PreteurClawbackDue)
    {
        public static readonly FloorEndModifierConsumptionResult None = new(false, false);
    }
}
