namespace Leds.GameEngine.Domain.Runs;

public enum RunModifierType
{
    StartingGuardBonus = 0,
    NextCombatDifficultyMultiplier = 1,
    RewardPowerMultiplierBonus = 2,
    PermanentCombatDifficultyBonus = 3,
    CombatDifficultyMultiplier = 4,
    RewardPowerMultiplier = 5,
    AttackPowerBonus = 6,
    DefenseBonus = 7,
    SpeedBonus = 8,
    InitiativeBonus = 9,
    FocusBonus = 11,
    ManaBonus = 12,
    ChargeBonus = 13,
    RoomClimate = 14,
    AttackTypeOverride = 15,

    // Lois du Palais — mécaniques introduites par le Compendium des Lois (chapitres IV/VIII/IX).
    // Ajoutés en fin de liste uniquement : les valeurs sont sérialisées par nom (voir
    // RunModifierEntity.Type), mais on évite quand même de réordonner par prudence.
    TurnOrderLock = 16,
    TurnOrderReverse = 17,

    /// <summary>"Loi de la Curée" (law.curee) — +15% damage taken while below 25% max
    /// HP, symmetric across both sides. Renamed from the never-seeded "ExecuteThreshold"
    /// placeholder, which wrongly assumed an instant-kill mechanic (safe to rename in
    /// place — never persisted, and RunModifierEntity.Type is stored by name anyway).</summary>
    DamageAmplificationBelowHpThreshold = 18,
    RoomTraversalHpDrain = 19,
    HitCounterDoubleDamage = 20,
    MirrorCombatCopy = 21,
    SuspendSevereLaws = 22,

    /// <summary>"Loi de la Première Impression" (law.premiere-impression) — the combat's
    /// first landed hit, any side, is forced critical. See Combat.FirstHitCriticalEnabled.</summary>
    FirstHitCritical = 23,

    /// <summary>"Loi de l'Écriture" (law.ecriture) — every DamageOverTime effect (both sides)
    /// lasts N extra turns. Value is the bonus turn count (converted to ticks at
    /// CombatFactory time via CombatTime.TicksPerTurn), not a raw tick count.</summary>
    DotDurationExtension = 24,

    /// <summary>"Loi du Duel" (law.duel) — mono-target skills deal +20% damage, AoE skills
    /// deal -20%, both sides. See Combat.DuelDamageAsymmetryEnabled.</summary>
    DuelDamageAsymmetry = 25,

    /// <summary>"Loi de la Destinée" (law.destinee) — every combatant, both sides, receives
    /// the exact "Une destinée cruelle" bundle (canon.skill.destinee-cruelle) for the room:
    /// +20% Attack/Defense/Speed/Focus and a 10%-max-HP DoT with no end.
    /// Applied once at CombatFactory time (like TurnOrderReverse) — no Combat-level flag
    /// needed since nothing has to be checked live during the fight.</summary>
    CruelDestinyForEveryone = 26,

    /// <summary>"Édit du Souvenir Doux" (law.souvenir-doux) — all healing RECEIVED by the
    /// player's own team (allies only, unlike the Chapitre II climate bundles which are
    /// symmetric) is boosted +20% for the floor. See CombatFactory.ApplyAllyHealingBonus.</summary>
    AllyHealingBonus = 27,

    /// <summary>"Loi du Nom Retenu" (law.nom-retenu) — every NPC reputation change for the
    /// floor (both gains AND losses/transgressions) is doubled. See
    /// Run.ScaleReputationGain.</summary>
    ReputationChangeDoubled = 28,

    /// <summary>"Loi du Témoin" (law.temoin) — armed NPC wounds cannot be soothed for the
    /// floor, neither by a dialogue act ("SootheWound") nor by score threshold
    /// ("RefreshWounds") — worsening (arming/rupturing) is unaffected. See
    /// NpcEventChoiceResolver's IsWoundHealingBlocked check.</summary>
    WoundHealingBlocked = 29,

    /// <summary>"Loi du Silence Dû" (law.silence-du) — both sides: Physical-category
    /// skills deal +8% damage, and every Player-side skill cast costs +2 flat mana. See
    /// CombatFactory.ApplySilenceDuBundle.</summary>
    SilenceDuActive = 30,

    /// <summary>"Loi des Poches Cousues" (law.poches-cousues) — no consumable can be used
    /// while in combat; outside combat, consumable effects are boosted +25%. See
    /// Run.UseItem.</summary>
    ConsumablesRestrictedInCombat = 31,

    /// <summary>"Loi de l'Éloge Funèbre" (law.eloge-funebre) — after any combatant (either
    /// side) is defeated, the next combatant to act may only use the basic attack. See
    /// Combat.RegisterCombatantDefeated/NextActionRestrictedToBasicAttack.</summary>
    PostDeathBasicAttackOnly = 32,

    /// <summary>"Loi du Tapis Propre" (law.tapis-propre) — each combatant's (either side)
    /// first turn of combat cannot be a Damage-type skill; support/buff/debuff/movement
    /// only. See Combatant.HasActedThisCombat/CombatSkillActionValidator.</summary>
    TapisPropreEnabled = 33,

    /// <summary>"Loi de la Troisième Tasse" (law.troisieme-tasse) — every heal
    /// application (skill or item) has a 10% chance to be "served in the third cup":
    /// halved, with a light poison DoT applied instead. See
    /// Combat.ApplyThirdCupRollIfActive.</summary>
    ThirdCupHealCorruptionEnabled = 34,

    /// <summary>"Loi de l'Abondance" (law.abondance) — item-node reward offers propose
    /// 4 choices instead of 3. See RewardOfferFactory.CreateItemRewardOffer. The SFD's
    /// "un nœud sur deux est vide à l'ouverture" half is NOT modeled (documented gap —
    /// no zero-choice RewardOffer flow exists).</summary>
    AbondanceExtraChoiceEnabled = 35,

    /// <summary>"Loi des Présentations" (law.presentations) — each enemy's first action
    /// of the combat is announced (telegraphed) via a log entry immediately before it
    /// resolves. See EnemyCombatTurnResolver.Resolve. Documented simplification: the SFD
    /// says "au premier tour de chaque combat, tous les ennemis annoncent" (a single
    /// batch announcement at combat start) — this is approximated as each enemy
    /// announcing individually right before their own first action, since no channel
    /// exists to surface log entries before combat's first response.</summary>
    PresentationsEnabled = 36,

    /// <summary>"Loi du Miroir" (law.miroir) — the first skill cast by the player's team
    /// in each combat is immediately copied by the fastest living enemy (same values,
    /// targeting re-resolved from the copying enemy's side — see
    /// Combat.TryConsumeMirrorTrigger/GetFastestLivingEnemy and
    /// UseCombatSkillCommandHandler.ResolveMirrorCopyIfTriggered).</summary>
    MiroirEnabled = 37,

    /// <summary>"Loi de l'Invitation" (law.invitation) — combat loot item drop chances
    /// are boosted by this percentage (e.g. 10 → +10%), applied multiplicatively to
    /// each loot table entry's DropPercent. See EnemyLootRewardBuilder.RollIndependent.
    /// Value is meaningful (the bonus percent itself), not a mere gate. Documented gap:
    /// the SFD's "tout butin est majoré... Éclats" half (a +10% currency bonus) is NOT
    /// modeled — no combat-loot currency mechanic exists in the engine at all (Éclats
    /// are only ever awarded via NPC rare-offering claims). The SFD's "impossible de
    /// fuir les combats" restriction is likewise not modeled, but requires no code: no
    /// combat-flee action exists anywhere in the engine to begin with, so the
    /// restriction is vacuously already true.</summary>
    LootChanceBonusPercent = 38,

    /// <summary>"Loi de l'Oubli Partiel" (law.oubli-partiel) — gates the floor-scoped
    /// forgotten-skill restriction. The actual forgotten skill KEY cannot live on this
    /// modifier (Value is a plain double, no string payload) — it is picked once at
    /// promulgation time and stored directly on Run.ForgottenSkillKey (see
    /// Run.PickForgottenSkill, called from ActivatePalaceLaw), then baked into Combat
    /// the same way other Run-level flags are (see CombatFactory). This modifier's
    /// only job is to ride the existing UntilFloorEnds consumption/cumul-cap plumbing;
    /// Run.ConsumeFloorEndModifiers clears ForgottenSkillKey when it is consumed.</summary>
    SkillForgotten = 39,

    /// <summary>"Loi de l'Impôt du Seuil" (law.impot-seuil) — cost in "Éclats du Palais"
    /// charged at the entry of every room while the law is active (SFD: 5). See
    /// MoveToNextRoomCommandHandler, which reads this magnitude and calls
    /// IPlayerProfileGateway.TrySpendCurrencyAsync at each room transition.</summary>
    RoomTollAmount = 40,

    /// <summary>"Loi de l'Impôt du Seuil" insolvency penalty — a stacking, floor-scoped
    /// reduction to the whole team's max HP (SFD: -2% per unpaid room toll, cumulable).
    /// Not granted by PalaceLawMapper/ActivatePalaceLaw like other law effects — applied
    /// directly via Run.ApplyRoomTollInsolvencyDebuff whenever a room-toll payment fails.
    /// Summed and applied as a multiplier in CombatFactory.CreateFromDraft.</summary>
    MaxHpReductionPercent = 41,

    /// <summary>"Loi du Prêteur" (law.preteur) — currency gains from NPC offerings are
    /// boosted by this percentage (SFD: +50%). See NpcEventChoiceResolver.ApplyOfferingAsync.
    /// Also doubles as this law's "active" marker for the floor-end 25% clawback (see
    /// Run.ConsumeFloorEndModifiers / MoveToNextRoomCommandHandler).</summary>
    CurrencyGainBonusPercent = 42,

    /// <summary>"Loi de la Chandelle" (law.chandelle) — one free reroll of an item-node
    /// reward offer for the floor. One modifier instance = one charge, consumed on use
    /// (not swept in bulk at floor end like other UntilFloorEnds modifiers — see
    /// Run.TryConsumeItemNodeRerollCharge, called from RerollItemRewardOfferCommandHandler).
    /// Unused charges still expire normally at floor end via ConsumeFloorEndModifiers.</summary>
    ItemNodeRerollCharge = 43,

    /// <summary>
    /// Encodes an explicit multi-room weather plan as climate*100 + rooms remaining.
    /// It survives normal room climate replacement and is consumed one room at a time.
    /// </summary>
    ForcedWeatherPlan = 44,

    /// <summary>"Édit des Portes Ouvertes" (law.portes-ouvertes) — gates
    /// GetUpcomingRoomsQuery: while active, the player can query the names of the
    /// remaining rooms in the current floor. See IRunGenerator.PreviewUpcomingRoomNamesAsync
    /// — the room *identities* for the rest of a floor are already fully deterministic
    /// from (seed, world graph, visited room keys), so this reveals real upcoming names,
    /// not a guess; only each room's internal grid/nodes stay hidden until actually
    /// entered.</summary>
    UpcomingRoomNamesRevealEnabled = 45,
}
