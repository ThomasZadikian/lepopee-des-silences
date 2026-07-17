using Leds.GameEngine.Domain.Combats.Atb;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Domain.Combats;

public sealed class Combat
{
    private Combat(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        CombatStatus status,
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies,
        CombatantId? activeCombatantId,
        int turnNumber,
        int currentTick,
        DateTime createdAtUtc,
        int hitCounter,
        bool hitCounterDoubleDamageEnabled,
        bool firstHitCriticalEnabled,
        bool hasFirstHitLanded,
        bool lowHpDamageAmplificationEnabled,
        int dotDurationExtensionTicks,
        bool duelDamageAsymmetryEnabled,
        int dotMagnitudeBonus,
        bool healingBlocked,
        bool falaiseWindEnabled = false,
        bool postDeathBasicAttackOnlyEnabled = false,
        bool nextActionRestrictedToBasicAttack = false,
        bool tapisPropreEnabled = false,
        bool thirdCupHealCorruptionEnabled = false)
    {
        Id = id;
        RunId = runId;
        RoomId = roomId;
        NodeId = nodeId;
        Status = status;
        Allies = allies;
        Enemies = enemies;
        ActiveCombatantId = activeCombatantId;
        TurnNumber = turnNumber;
        CurrentTick = currentTick;
        CreatedAtUtc = createdAtUtc;
        HitCounter = hitCounter;
        HitCounterDoubleDamageEnabled = hitCounterDoubleDamageEnabled;
        FirstHitCriticalEnabled = firstHitCriticalEnabled;
        HasFirstHitLanded = hasFirstHitLanded;
        LowHpDamageAmplificationEnabled = lowHpDamageAmplificationEnabled;
        DotDurationExtensionTicks = dotDurationExtensionTicks;
        DuelDamageAsymmetryEnabled = duelDamageAsymmetryEnabled;
        DotMagnitudeBonus = dotMagnitudeBonus;
        HealingBlocked = healingBlocked;
        FalaiseWindEnabled = falaiseWindEnabled;
        PostDeathBasicAttackOnlyEnabled = postDeathBasicAttackOnlyEnabled;
        NextActionRestrictedToBasicAttack = nextActionRestrictedToBasicAttack;
        TapisPropreEnabled = tapisPropreEnabled;
        ThirdCupHealCorruptionEnabled = thirdCupHealCorruptionEnabled;
    }

    public CombatId Id { get; }
    public RunId RunId { get; }
    public RoomId RoomId { get; }
    public NodeId NodeId { get; }
    public CombatStatus Status { get; private set; }
    public IReadOnlyCollection<Combatant> Allies { get; }
    public IReadOnlyCollection<Combatant> Enemies { get; }
    public CombatantId? ActiveCombatantId { get; private set; }
    public int TurnNumber { get; private set; }

    /// <summary>The ATB clock for this combat (advances in deterministic ticks).</summary>
    public int CurrentTick { get; private set; }

    public DateTime CreatedAtUtc { get; }
    public bool HasLivingAllies => Allies.Any(a => !a.IsDefeated);

    public bool HasLivingEnemies => Enemies.Any(e => !e.IsDefeated);

    private IEnumerable<Combatant> AllCombatants => Allies.Concat(Enemies);

    /// <summary>"Loi du Treizième Coup" — every 13th landed hit (all sides combined).</summary>
    public const int HitCounterTrigger = 13;

    /// <summary>Total landed hits (all sides combined) so far this combat.</summary>
    public int HitCounter { get; private set; }

    /// <summary>True when the "Loi du Treizième Coup" law is active for this combat,
    /// baked in at creation from the run's active RunModifiers.</summary>
    public bool HitCounterDoubleDamageEnabled { get; }

    /// <summary>
    /// "Loi du Treizième Coup" (law.treizieme-coup): "toutes les douze frappes, le
    /// Palais en réclame une" — records a landed hit and returns true when it is the
    /// 13th (and every 13th thereafter) landed hit of the combat, all sides combined.
    /// The caller must then double that hit's damage. Simplified: the hit's own natural
    /// attacker/target is treated as the "bénéficiaire" rather than reassigning the bonus
    /// to an unrelated random combatant — no such reassignment mechanic exists in the
    /// single-hit damage pipeline (documented simplification).
    /// </summary>
    public bool RegisterLandedHit()
    {
        HitCounter++;
        return HitCounterDoubleDamageEnabled && HitCounter % HitCounterTrigger == 0;
    }

    /// <summary>True when the "Loi de la Première Impression" law is active for this
    /// combat, baked in at creation from the run's active RunModifiers.</summary>
    public bool FirstHitCriticalEnabled { get; }

    /// <summary>Whether the combat's very first landed hit (any side) has already
    /// been resolved — regardless of whether the law was active to force it critical.</summary>
    public bool HasFirstHitLanded { get; private set; }

    /// <summary>
    /// "Loi de la Première Impression" (law.premiere-impression): "le tout premier
    /// coup porté dans chaque combat, quel qu'en soit l'auteur, est automatiquement
    /// critique." Combat-scoped (not combatant-scoped): whichever side lands the
    /// very first hit benefits, regardless of who that is. Returns true exactly once
    /// per combat, the moment the first hit lands.
    /// </summary>
    public bool TryConsumeFirstHitCritical()
    {
        if (HasFirstHitLanded)
            return false;

        HasFirstHitLanded = true;
        return FirstHitCriticalEnabled;
    }

    /// <summary>"Loi de la Curée" (law.curee): "+15% dégâts subis pour tout combattant
    /// sous 25% de ses PV max" — a symmetric damage-taken amplifier, baked in at
    /// creation from the run's active RunModifiers. Checked live against the
    /// TARGET's current vitality on every hit (see CombatSkillEffectResolver), since
    /// the threshold moves with HP throughout the fight.</summary>
    public bool LowHpDamageAmplificationEnabled { get; }

    /// <summary>"Loi de la Curée" HP threshold, as a percent of max vitality.</summary>
    public const int LowHpDamageAmplificationThresholdPercent = 25;

    /// <summary>"Loi de la Curée" damage-taken bonus, in percent.</summary>
    public const int LowHpDamageAmplificationBonusPercent = 15;

    /// <summary>"Loi de l'Écriture" (law.ecriture): "tous les DoT (des deux camps) durent
    /// +2 tours" — bonus ticks added to every newly-applied DamageOverTime effect's
    /// duration (see CombatSkillEffectResolver.ApplyStatusEffectSpec). Baked in at
    /// creation as ticks (already converted from the law's "turns" magnitude via
    /// AtbConstants.TicksPerTurn), zero when the law is not active.</summary>
    public int DotDurationExtensionTicks { get; }

    /// <summary>"Loi du Duel" (law.duel): "les attaques et sorts mono-cibles infligent
    /// +20% ; les attaques et sorts de zone infligent -20%" — symmetric across both
    /// sides, baked in at creation from the run's active RunModifiers.</summary>
    public bool DuelDamageAsymmetryEnabled { get; }

    /// <summary>"Loi du Duel" mono-target damage bonus, in percent.</summary>
    public const int DuelSingleTargetBonusPercent = 20;

    /// <summary>"Loi du Duel" area-of-effect damage penalty, in percent.</summary>
    public const int DuelAreaOfEffectPenaltyPercent = 20;

    /// <summary>"Loi de la Marée Haute" (law.maree-haute, Pluie violacée climate): "tous
    /// les DoT (joueur et ennemis) infligent +1 dégât par tour" — a flat magnitude bonus
    /// added to every newly-applied DamageOverTime effect (see CombatSkillEffectResolver.
    /// ApplyStatusEffectSpec). Baked in at creation from the active climate, zero
    /// otherwise. Generic (not climate-specific by name) so any future law can reuse it.</summary>
    public int DotMagnitudeBonus { get; }

    /// <summary>"Loi des Visites Terminées" (law.visites-terminees, liée à room.hopital):
    /// "les sorts de soin sont sans effet dans cette salle (les soins par objets
    /// fonctionnent)" — gates CombatSkillEffectResolver's "Heal" skill-effect branch only;
    /// item-based healing (UseItemInCombatCommandHandler) is a separate code path and is
    /// untouched. Baked in at creation from the room's CatalogRoomKey.</summary>
    public bool HealingBlocked { get; }

    /// <summary>"Loi de la Falaise" (law.falaise, liée à room.falaise): "à chaque tour de
    /// combat, 10% de chance qu'un combattant aléatoire soit repoussé d'un rang. Les
    /// combattants déjà au rang arrière subissent 5 dégâts (les embruns)." Baked in at
    /// creation from the room's CatalogRoomKey (same convention as HealingBlocked);
    /// resolved once per <see cref="AdvanceTurn"/> — see <see cref="ApplyFalaiseWindIfActive"/>.
    /// No combat-log entry surfaces the trigger today (documented simplification):
    /// only the row/vitality state changes.</summary>
    public bool FalaiseWindEnabled { get; }

    /// <summary>"Loi de l'Éloge Funèbre" (law.eloge-funebre): "à chaque mort (alliée ou
    /// ennemie), le combattant suivant dans l'ordre d'action ne peut effectuer qu'une
    /// attaque basique." Baked in at creation from the run's active RunModifiers (a
    /// normal ambient-drawn law, unlike the room-bound flags above).</summary>
    public bool PostDeathBasicAttackOnlyEnabled { get; }

    /// <summary>
    /// Mutable runtime state for "Loi de l'Éloge Funèbre": true after a combatant has
    /// been defeated and the next actor's action has not yet been validated. Set by
    /// <see cref="RegisterCombatantDefeated"/>, consumed by
    /// <see cref="ConsumeBasicAttackRestriction"/> once a valid action resolves — only
    /// the basic attack ("skill.basic.strike") can pass validation while this is true
    /// (see CombatSkillActionValidator). Documented simplification: item use
    /// (UseItemInCombatCommand) and the Reposition action are NOT gated by this
    /// restriction — only skill-based actions are, since both other paths never call
    /// through the shared skill-action validator.
    /// </summary>
    public bool NextActionRestrictedToBasicAttack { get; private set; }

    /// <summary>Records that a combatant was just defeated; arms the "Loi de l'Éloge
    /// Funèbre" restriction for whoever acts next. No-op when the law is inactive.</summary>
    public void RegisterCombatantDefeated()
    {
        if (PostDeathBasicAttackOnlyEnabled)
        {
            NextActionRestrictedToBasicAttack = true;
        }
    }

    /// <summary>Clears the "Loi de l'Éloge Funèbre" restriction once a valid action has
    /// consumed it (called on every successful validation, a no-op when already clear).</summary>
    public void ConsumeBasicAttackRestriction()
    {
        NextActionRestrictedToBasicAttack = false;
    }

    /// <summary>"Loi du Tapis Propre" (law.tapis-propre): baked in at creation from the
    /// run's active RunModifiers. Unlike Éloge Funèbre above, no mutable combat-wide
    /// state is needed — the restriction is tracked per-combatant via
    /// Combatant.HasActedThisCombat and enforced in CombatSkillActionValidator.</summary>
    public bool TapisPropreEnabled { get; }

    /// <summary>"Loi de la Troisième Tasse" (law.troisieme-tasse): baked in at creation
    /// from the run's active RunModifiers. Every heal application (skill or item) rolls
    /// <see cref="ApplyThirdCupRollIfActive"/> — no mutable combat-wide state, the roll
    /// is purely per-application and per-target.</summary>
    public bool ThirdCupHealCorruptionEnabled { get; }

    /// <summary>"Loi de la Troisième Tasse" per-application chance.</summary>
    private const double ThirdCupChance = 0.10;

    /// <summary>"Loi de la Troisième Tasse" heal multiplier when the cup is corrupted
    /// ("il ne restaure que la moitié").</summary>
    private const double ThirdCupHealMultiplier = 0.5;

    /// <summary>"Loi de la Troisième Tasse" poison ("léger") per-turn damage.</summary>
    private const int ThirdCupPoisonDamagePerTurn = 3;

    /// <summary>"Loi de la Troisième Tasse" poison duration, in "tours" (combat turns).</summary>
    private const int ThirdCupPoisonDurationTurns = 4;

    /// <summary>
    /// Rolls "Loi de la Troisième Tasse" for one heal application (skill or item):
    /// 10% chance the heal is "served in the third cup" — halved, with a light poison
    /// DoT applied to the target. No-op (returns <paramref name="healAmount"/> unchanged,
    /// triggered=false) when the law is inactive. Called once per target per heal
    /// application by both CombatSkillEffectResolver.ResolveHeal and
    /// UseItemInCombatCommandHandler.ApplyItemEffect.
    /// </summary>
    public (int HealAmount, bool Triggered) ApplyThirdCupRollIfActive(Combatant target, int healAmount)
    {
        if (!ThirdCupHealCorruptionEnabled)
            return (healAmount, false);

        var seed = $"troisieme-tasse|{Id.Value:N}|{target.Id.Value:N}|{CurrentTick}";
        if (DeterministicCombatRoll.UnitInterval(seed) >= ThirdCupChance)
            return (healAmount, false);

        target.ApplyStatusEffect(CombatStatusEffect.Create(
            key: "troisieme-tasse-poison",
            displayName: "Poison léger (Troisième Tasse)",
            kind: StatusEffectKind.DamageOverTime,
            currentTick: CurrentTick,
            durationTicks: AtbConstants.TicksPerTurn * ThirdCupPoisonDurationTurns,
            magnitude: ThirdCupPoisonDamagePerTurn,
            stacks: 1,
            tickInterval: AtbConstants.TicksPerTurn));

        var corrupted = Math.Max(1, (int)Math.Round(healAmount * ThirdCupHealMultiplier, MidpointRounding.AwayFromZero));
        return (corrupted, true);
    }

    /// <summary>"Loi de la Falaise" random-target chance, checked once per turn.</summary>
    private const double FalaiseWindTriggerChance = 0.10;

    /// <summary>"Loi de la Falaise" fallback damage ("les embruns") when the randomly
    /// picked combatant is already in the Back row and cannot be pushed further.</summary>
    private const int FalaiseWindDamage = 5;

    public static Combat Create(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies,
        bool hitCounterDoubleDamageEnabled = false,
        bool firstHitCriticalEnabled = false,
        bool lowHpDamageAmplificationEnabled = false,
        int dotDurationExtensionTicks = 0,
        bool duelDamageAsymmetryEnabled = false,
        int dotMagnitudeBonus = 0,
        bool healingBlocked = false,
        bool falaiseWindEnabled = false,
        bool postDeathBasicAttackOnlyEnabled = false,
        bool tapisPropreEnabled = false,
        bool thirdCupHealCorruptionEnabled = false)
    {
        if (id.Value == Guid.Empty)
            throw new DomainException("Combat id is required.");

        if (!allies.Any())
            throw new DomainException("Combat requires at least one ally.");

        if (!enemies.Any())
            throw new DomainException("Combat requires at least one enemy.");

        if (allies.Any(a => a.IsDefeated))
            throw new DomainException("Combat cannot start with a defeated ally.");

        if (enemies.Any(e => e.IsDefeated))
            throw new DomainException("Combat cannot start with a defeated enemy.");

        // A valid opening actor (initiative order). The ATB preparer re-runs the
        // scheduler once fill rates/opening gauges are baked to set the real opener.
        var openingOrder = OrderByInitiative(allies.Concat(enemies));

        return new Combat(
            id,
            runId,
            roomId,
            nodeId,
            CombatStatus.Active,
            allies.ToArray(),
            enemies.ToArray(),
            activeCombatantId: openingOrder[0].Id,
            turnNumber: 1,
            currentTick: 0,
            createdAtUtc: DateTime.UtcNow,
            hitCounter: 0,
            hitCounterDoubleDamageEnabled: hitCounterDoubleDamageEnabled,
            firstHitCriticalEnabled: firstHitCriticalEnabled,
            hasFirstHitLanded: false,
            lowHpDamageAmplificationEnabled: lowHpDamageAmplificationEnabled,
            dotDurationExtensionTicks: dotDurationExtensionTicks,
            duelDamageAsymmetryEnabled: duelDamageAsymmetryEnabled,
            dotMagnitudeBonus: dotMagnitudeBonus,
            healingBlocked: healingBlocked,
            falaiseWindEnabled: falaiseWindEnabled,
            postDeathBasicAttackOnlyEnabled: postDeathBasicAttackOnlyEnabled,
            tapisPropreEnabled: tapisPropreEnabled,
            thirdCupHealCorruptionEnabled: thirdCupHealCorruptionEnabled);
    }

    public void MarkCompleted()
    {
        if (Status != CombatStatus.Active)
            throw new DomainException("Only an active combat can be completed.");

        Status = CombatStatus.Completed;
        ActiveCombatantId = null;
    }

    public void MarkFailed()
    {
        if (Status != CombatStatus.Active)
            throw new DomainException("Only an active combat can be failed.");

        Status = CombatStatus.Failed;
        ActiveCombatantId = null;
    }

    public Combatant? GetActiveCombatant()
    {
        if (Status != CombatStatus.Active || ActiveCombatantId is null)
            return null;

        return AllCombatants.FirstOrDefault(c => c.Id == ActiveCombatantId && !c.IsDefeated);
    }

    public void EnsureActorCanAct(Guid actorId)
    {
        if (Status != CombatStatus.Active)
            throw new DomainException("Combat is not active.");

        var actor = AllCombatants.FirstOrDefault(c => c.Id.Value == actorId)
            ?? throw new DomainException("Actor does not exist in this combat.");

        if (actor.IsDefeated)
            throw new DomainException("Defeated combatants cannot act.");

        if (ActiveCombatantId is null || actor.Id != ActiveCombatantId)
            throw new DomainException("It is not this combatant's turn.");
    }

    public void CompleteIfAllEnemiesDefeated()
    {
        if (Status == CombatStatus.Active && !HasLivingEnemies)
        {
            Status = CombatStatus.Completed;
            ActiveCombatantId = null;
        }
    }

    private const string PlayerSourceKey = "player.self";

    /// <summary>
    /// The protagonist — the only combatant whose death ends the run. Other
    /// Player-side allies are companions. Identified by source key, with a fallback
    /// to the first Player-side ally (always the protagonist, built first).
    /// </summary>
    public Combatant? GetPlayerCombatant()
        => Allies.FirstOrDefault(a => string.Equals(a.SourceKey, PlayerSourceKey, StringComparison.OrdinalIgnoreCase))
           ?? Allies.FirstOrDefault(a => a.Side == CombatantSide.Player);

    /// <summary>True when the protagonist is defeated (or absent).</summary>
    public bool IsPlayerDefeated
    {
        get
        {
            var player = GetPlayerCombatant();
            return player is null || player.IsDefeated;
        }
    }

    /// <summary>
    /// Ends the combat as a loss when the PLAYER (protagonist) is defeated.
    /// Companions dying does NOT end the run — only the player's death is a game
    /// over, even with allies still standing. (Name kept for call-site stability.)
    /// </summary>
    public void FailIfAllAlliesDefeated()
    {
        if (Status == CombatStatus.Active && IsPlayerDefeated)
        {
            Status = CombatStatus.Failed;
            ActiveCombatantId = null;
        }
    }

    /// <summary>
    /// Advances the ATB clock to the next ready combatant and makes it active.
    /// Call <see cref="RegisterActionTaken"/> first (after an action resolves) so the
    /// actor's gauge is consumed and its recovery is set before the clock advances.
    /// </summary>
    public void AdvanceTurn()
    {
        CompleteIfAllEnemiesDefeated();
        FailIfAllAlliesDefeated();
        if (Status != CombatStatus.Active)
            return;

        RecalculateAllTempo();

        // The combatant that just had its turn spends its gauge before the clock
        // advances, so the scheduler elects the NEXT ready combatant rather than
        // re-selecting the one that just acted.
        var outgoing = AllCombatants.FirstOrDefault(c => c.Id == ActiveCombatantId);
        if (outgoing is not null && !outgoing.IsDefeated && outgoing.AtbGauge >= AtbConstants.ReadyThreshold)
            outgoing.SetAtbGauge(0);

        var combatants = AllCombatants.ToArray();

        var participants = combatants
            .Select(c => new AtbParticipant(
                CombatantId: c.Id.Value,
                Gauge: c.AtbGauge,
                FillPerTick: c.AtbFillPerTick,
                RecoveryUntilTick: c.AtbRecoveryUntilTick,
                Initiative: c.BaseStatSnapshot.Initiative,
                IsActive: !c.IsDefeated))
            .ToArray();

        var result = AtbScheduler.Advance(participants, CurrentTick);

        foreach (var participant in result.Participants)
        {
            var combatant = combatants.First(c => c.Id.Value == participant.CombatantId);
            if (!combatant.IsDefeated)
                combatant.SetAtbGauge(participant.Gauge);
        }

        CurrentTick = (int)Math.Min(result.CurrentTick, int.MaxValue);

        if (result.NextActorId is null)
        {
            Status = CombatStatus.Failed;
            ActiveCombatantId = null;
            return;
        }

        ActiveCombatantId = new CombatantId(result.NextActorId.Value);
        TurnNumber++;

        GetActiveCombatant()?.ResetGuardToBase();

        ApplyFalaiseWindIfActive();
    }

    /// <summary>
    /// "Loi de la Falaise": each turn advance, a 10%-chance deterministic roll (seeded by
    /// combat id + turn number, so replays/rehydration reproduce the same outcome) picks
    /// a random living combatant. A Front-row target is pushed to Back; a target already
    /// in Back row instead takes <see cref="FalaiseWindDamage"/> vitality damage
    /// (bypassing Guard — "les embruns" are an environmental effect, not an attack).
    /// No-op when <see cref="FalaiseWindEnabled"/> is false.
    /// </summary>
    private void ApplyFalaiseWindIfActive()
    {
        if (!FalaiseWindEnabled || Status != CombatStatus.Active)
            return;

        var living = AllCombatants.Where(c => !c.IsDefeated).ToArray();
        if (living.Length == 0)
            return;

        var triggerSeed = $"falaise-wind|{Id.Value:N}|{TurnNumber}";
        if (DeterministicCombatRoll.UnitInterval(triggerSeed) >= FalaiseWindTriggerChance)
            return;

        var pickSeed = $"falaise-wind-target|{Id.Value:N}|{TurnNumber}";
        var index = Math.Min(
            (int)(DeterministicCombatRoll.UnitInterval(pickSeed) * living.Length),
            living.Length - 1);
        var target = living[index];

        if (target.Row == CombatRow.Front)
        {
            target.SetRow(CombatRow.Back);
        }
        else
        {
            target.ApplyVitalityDamage(FalaiseWindDamage);
            CompleteIfAllEnemiesDefeated();
            FailIfAllAlliesDefeated();
        }
    }

    /// <summary>
    /// Interruption / stagger: pushes the target's ATB gauge back (larger push and
    /// charge loss if the target was charging).
    /// </summary>
    public void ApplyAtbInterruption(Guid targetId)
    {
        var target = AllCombatants.FirstOrDefault(c => c.Id.Value == targetId);
        if (target is null || target.IsDefeated)
            return;

        target.SetAtbGauge(AtbActionMath.Interrupt(target.AtbGauge));
    }

    /// <summary>
    /// "Time flows" while the player holds: advances the clock by a fixed delta,
    /// filling every combatant. Once an ally holds a ready selection, every
    /// ally's gauge freezes there (a ready gauge does not climb further, so
    /// they can't "steal" the next turn while waiting). Enemies always keep
    /// filling — time still flows for them. Does not change the active
    /// combatant. Returns the ids of enemies that have become ready to act,
    /// readiest first — the caller resolves them while the player keeps holding.
    /// </summary>
    public IReadOnlyCollection<Guid> HoldTick(int deltaTicks)
    {
        if (Status != CombatStatus.Active || deltaTicks <= 0)
            return [];

        RecalculateAllTempo();

        // While an ally holds the selection (an ally is active AND ready), every
        // ally's gauge freezes — none can climb further or "steal" the next turn.
        var active = GetActiveCombatant();
        var allyHoldsSelection = active is { Side: CombatantSide.Player }
            && active.AtbGauge >= AtbConstants.ReadyThreshold;

        foreach (var combatant in AllCombatants.Where(c => !c.IsDefeated))
        {
            combatant.DecayTempoMomentum(deltaTicks);

            if (allyHoldsSelection && combatant.Side == CombatantSide.Player)
                continue; // frozen during selection

            if (combatant.IsAtbLocked)
                continue; // stun / ATB-lock freezes the gauge

            var recoveryWait = Math.Max(0, combatant.AtbRecoveryUntilTick - CurrentTick);
            var fillTicks = Math.Max(0, deltaTicks - recoveryWait);
            if (fillTicks <= 0)
                continue;

            var raw = (long)combatant.AtbGauge + (long)combatant.AtbFillPerTick * fillTicks;
            combatant.SetAtbGauge((int)Math.Min(raw, AtbConstants.ReadyThreshold));
        }

        CurrentTick += deltaTicks;
        return Enemies
            .Where(e => !e.IsDefeated && !e.IsStunned && !e.IsSilenced && e.AtbGauge >= AtbConstants.ReadyThreshold)
            .OrderByDescending(e => e.AtbGauge)
            .ThenByDescending(e => e.BaseStatSnapshot.Initiative)
            .ThenBy(e => e.Id.Value)
            .Select(e => e.Id.Value)
            .ToArray();
    }

    /// <summary>
    /// Advances every combatant's durable status effects to the current tick: applies
    /// due DoT/HoT and removes expired effects. Returns what happened (for combat logs).
    /// Call right after <see cref="HoldTick"/> so effects resolve as time passes.
    /// </summary>
    public IReadOnlyCollection<CombatantStatusTick> TickAllStatusEffects()
    {
        var ticks = new List<CombatantStatusTick>();

        foreach (var combatant in AllCombatants.ToArray())
        {
            var wasAlive = !combatant.IsDefeated;

            foreach (var ev in combatant.TickStatusEffects(CurrentTick))
                ticks.Add(new CombatantStatusTick(combatant.Id.Value, combatant.DisplayName, ev));

            // "Loi de l'Éloge Funèbre" applies to any death, DoT-inflicted included.
            if (wasAlive && combatant.IsDefeated)
                RegisterCombatantDefeated();
        }

        // DoT may have defeated someone — re-evaluate combat end.
        CompleteIfAllEnemiesDefeated();
        FailIfAllAlliesDefeated();

        return ticks;
    }


    /// <summary>
    /// Forces a living combatant to be the active one. Used by the hold loop to
    /// resolve a specific ready enemy's turn while the player is holding.
    /// </summary>
    public void MakeActiveCombatant(Guid combatantId)
    {
        var target = AllCombatants.FirstOrDefault(c => c.Id.Value == combatantId && !c.IsDefeated);
        if (target is not null)
            ActiveCombatantId = target.Id;
    }

    public Combatant? GetNextActiveCombatant()
    {
        if (Status != CombatStatus.Active)
            return null;

        RecalculateAllTempo();

        var combatants = AllCombatants.ToArray();
        var participants = combatants
            .Select(c => new AtbParticipant(
                c.Id.Value, c.AtbGauge, c.AtbFillPerTick, c.AtbRecoveryUntilTick,
                c.BaseStatSnapshot.Initiative, !c.IsDefeated))
            .ToArray();

        var result = AtbScheduler.Advance(participants, CurrentTick);
        if (result.NextActorId is null)
            return null;

        return combatants.FirstOrDefault(c => c.Id.Value == result.NextActorId.Value);
    }

    /// <summary>
    /// Rehydrates a combat from a trusted persistence snapshot.
    /// This method must not be used to create a new gameplay combat.
    /// </summary>
    public static Combat Rehydrate(
        CombatId id,
        RunId runId,
        RoomId roomId,
        NodeId nodeId,
        CombatStatus status,
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies,
        CombatantId? activeCombatantId,
        int turnNumber,
        DateTime createdAtUtc,
        int currentTick = 0,
        int hitCounter = 0,
        bool hitCounterDoubleDamageEnabled = false,
        bool firstHitCriticalEnabled = false,
        bool hasFirstHitLanded = false,
        bool lowHpDamageAmplificationEnabled = false,
        int dotDurationExtensionTicks = 0,
        bool duelDamageAsymmetryEnabled = false,
        int dotMagnitudeBonus = 0,
        bool healingBlocked = false,
        bool falaiseWindEnabled = false,
        bool postDeathBasicAttackOnlyEnabled = false,
        bool nextActionRestrictedToBasicAttack = false,
        bool tapisPropreEnabled = false,
        bool thirdCupHealCorruptionEnabled = false)
    {
        return new Combat(id, runId, roomId, nodeId, status, allies, enemies, activeCombatantId, turnNumber, currentTick, createdAtUtc, hitCounter, hitCounterDoubleDamageEnabled, firstHitCriticalEnabled, hasFirstHitLanded, lowHpDamageAmplificationEnabled, dotDurationExtensionTicks, duelDamageAsymmetryEnabled, dotMagnitudeBonus, healingBlocked, falaiseWindEnabled, postDeathBasicAttackOnlyEnabled, nextActionRestrictedToBasicAttack, tapisPropreEnabled, thirdCupHealCorruptionEnabled);
    }

    /// <summary>
    /// Recomputes every living combatant's ATB fill rate from current EFFECTIVE
    /// stats and the opposing side's current average Speed. Called at the top of
    /// every clock-advancing operation so tempo reacts live to buffs, debuffs,
    /// equipment, and anything else that shifts Effective* stats mid-fight.
    /// </summary>
    public void RecalculateAllTempo()
    {
        foreach (var combatant in AllCombatants.Where(c => !c.IsDefeated))
        {
            combatant.RecalculateAtbFillPerTick(OpponentAverageEffectiveSpeed(combatant));
        }
    }

    private double OpponentAverageEffectiveSpeed(Combatant combatant)
    {
        var opposingSide = combatant.Side == CombatantSide.Player ? Enemies : Allies;
        var livingOpponents = opposingSide.Where(c => !c.IsDefeated).ToArray();

        // No living opponent (shouldn't normally happen mid-fight): neutral relative factor.
        return livingOpponents.Length > 0
            ? livingOpponents.Average(c => (double)c.EffectiveSpeed)
            : combatant.EffectiveSpeed;
    }

    private static Combatant[] OrderByInitiative(IEnumerable<Combatant> combatants)
    {
        return combatants
            .OrderByDescending(c => c.BaseStatSnapshot.Speed)
            .ThenByDescending(c => c.BaseStatSnapshot.Initiative)
            .ThenBy(c => c.Side)
            .ThenBy(c => c.Id.Value)
            .ToArray();
    }

    public void RegisterActionTaken(Guid actorId, int recoveryTicks)
    {
        var actor = AllCombatants.FirstOrDefault(c => c.Id.Value == actorId);
        if (actor is null)
            return;

        actor.RegisterAtbAction(CurrentTick, recoveryTicks);
        TurnNumber++; // each resolved action is its own "turn" (unique client log keys)
    }

    /// <summary>
    /// Re-elects the active combatant by CURRENT readiness — no time advance.
    /// Ally selection is STICKY: a living ally that already holds the active slot
    /// keeps it until it acts, so a faster companion's bar filling later cannot
    /// steal the selection. A fresh election prefers allies in party order
    /// (protagonist first), then enemies — never by gauge.
    /// </summary>
    public void ElectActiveByReadiness(Guid? preferredAllyId = null)
    {
        CompleteIfAllEnemiesDefeated();
        FailIfAllAlliesDefeated();

        if (Status != CombatStatus.Active)
        {
            ActiveCombatantId = null;
            return;
        }

        // Restore a still-ready HELD ally first. Enemy turns resolved within a tick
        // use the active slot as scratch space (MakeActiveCombatant) and must not
        // steal the player's selection: the caller passes the ally that held the
        // slot before the enemies acted so it is handed back.
        if (preferredAllyId is Guid preferredId)
        {
            var preferred = AllCombatants.FirstOrDefault(c => c.Id.Value == preferredId);
            if (preferred is { IsDefeated: false, Side: CombatantSide.Player }
                && !preferred.IsStunned
                && !preferred.IsSilenced
                && preferred.AtbGauge >= AtbConstants.ReadyThreshold)
            {
                ActiveCombatantId = preferred.Id;
                return;
            }
        }

        // Sticky: a living ALLY that already holds the active slot keeps it until it
        // acts. A companion whose bar fills later cannot steal the selection.
        var current = AllCombatants.FirstOrDefault(c => c.Id == ActiveCombatantId);
        if (current is { IsDefeated: false, Side: CombatantSide.Player }
            && !current.IsStunned
            && !current.IsSilenced
            && current.AtbGauge >= AtbConstants.ReadyThreshold)
        {
            return;
        }

        // Fresh election: first READY combatant in party order — allies first
        // (protagonist, then companions), then enemies. Never by gauge. Stunned/
        // silenced combatants are skipped (they cannot act).
        var elected = AllCombatants
            .FirstOrDefault(c => !c.IsDefeated && !c.IsStunned && !c.IsSilenced && c.AtbGauge >= AtbConstants.ReadyThreshold);

        var previousActiveId = ActiveCombatantId;
        ActiveCombatantId = elected?.Id;

        if (elected is not null && elected.Id != previousActiveId)
            elected.ResetGuardToBase();
    }
}