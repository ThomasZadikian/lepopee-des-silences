using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.GameEngine.Domain.Dialogue;
using Leds.GameEngine.Domain.Knowledge;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Protocol;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Infrastructure.Persistence.Mappers;

public static class RunPersistenceMapper
{
    // -----------------------------------------------------------------------
    // Domain → Entity
    // -----------------------------------------------------------------------

    public static RunEntity ToEntity(Run run)
    {
        var entity = new RunEntity
        {
            Id = run.Id.Value,
            PlayerId = run.PlayerId,
            Status = run.Status.ToString(),
            Outcome = run.Outcome?.ToString(),
            Revision = run.Revision,
            TechnicalRecoveryState = run.TechnicalRecoveryState.ToString(),
            ProgressionMode = run.ProgressionMode.ToString(),
            StoryDifficulty = run.StoryDifficulty?.ToString(),
            DifficultyLevel = run.DifficultyLevel?.Value,
            StorySequenceKey = run.StoryOverlay?.SequenceKey,
            StorySequenceVersion = run.StoryOverlay?.SequenceVersion,
            StoryStepKey = run.StoryOverlay?.StepKey,
            StoryCheckpointKey = run.StoryOverlay?.CheckpointKey,
            Seed = run.Seed,
            GeneratorVersion = run.GeneratorVersion,
            MarkovMatrixVersion = run.MarkovMatrixVersion,
            EmotionalAffinityMatrixVersion = run.EmotionalAffinityMatrix.Version,
            EmotionalAffinityMatrixJson = JsonSerializer.Serialize(run.EmotionalAffinityMatrix.Rules),
            CurrentRoomId = run.CurrentRoomId.Value,
            CurrentRoomIndex = run.CurrentRoomIndex,
            ActiveCombatId = run.ActiveCombatId?.Value,
            PendingRewardOfferId = run.PendingRewardOfferId?.Value,
            MaxHp = run.MaxHp,
            CurrentHp = run.CurrentHp,
            Attack = run.Attack,
            Defense = run.Defense,
            Speed = run.Speed,
            Focus = run.Focus,
            MagicAttack = run.MagicAttack,
            MagicDefense = run.MagicDefense,
            RunItemCapacity = run.RunItemCapacity,
            TypedDamageReductionsJson = run.TypedDamageReductions.Count > 0
                ? JsonSerializer.Serialize(run.TypedDamageReductions)
                : null,
            HitChanceBonusPercent = run.HitChanceBonusPercent,
            DotDurationReductionPercent = run.DotDurationReductionPercent,
            DotDamageReductionPercent = run.DotDamageReductionPercent,
            DotDamageBonusPercent = run.DotDamageBonusPercent,
            MagicDamageBonusPercent = run.MagicDamageBonusPercent,
            MagicDamageReductionPercent = run.MagicDamageReductionPercent,
            CriticalChanceBonusPercent = run.CriticalChanceBonusPercent,
            GuardBonusPercent = run.GuardBonusPercent,
            JournalEnabled = run.JournalEnabled,
            LawDenialEnabled = run.LawDenialEnabled,
            LawDenialLastUsedRoomIndex = run.LawDenialLastUsedRoomIndex,
            LastPromulgationFloorIndex = run.LastPromulgationFloorIndex,
            ForgottenSkillKey = run.ForgottenSkillKey,
            SuspendedSevereLawModifierIdsJson = run.SuspendedSevereLawModifierIds.Count > 0
                ? JsonSerializer.Serialize(run.SuspendedSevereLawModifierIds)
                : null,
            ReputationGainBonusPercent = run.ReputationGainBonusPercent,
            HimLitProtectionEnabled = run.HimLitProtectionEnabled,
            HealingBonusPercent = run.HealingBonusPercent,
            CaliceInfiniEnabled = run.CaliceInfiniEnabled,
            CaliceInfiniLastUsedRoomIndex = run.CaliceInfiniLastUsedRoomIndex,
            StartedAtUtc = run.StartedAt.UtcDateTime,
            EndedAtUtc = run.EndedAt?.UtcDateTime,
            SavedAtUtc = run.SavedAt?.UtcDateTime,
            PreSuspendStatus = run.PreSuspendStatus?.ToString(),
            ActiveNpcKey = run.ActiveNpcKey,
            NpcRelationshipsJson = SerializeNpcRelationships(run.NpcRelationships),
            KnowledgeEntriesJson = SerializeKnowledgeEntries(run.KnowledgeEntries),
            AmbientConversationStatesJson = SerializeAmbientConversationStates(run.AmbientConversationStates),
            CreatedAtUtc = run.StartedAt.UtcDateTime,
            UpdatedAtUtc = DateTime.UtcNow,
            Rooms = run.Rooms.Select(room => ToEntity(room, run.Id.Value)).ToList(),
            MemoryFragments = run.MemoryFragments
                .Select((key, index) => new RunMemoryFragmentEntity
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id.Value,
                    FragmentKey = key,
                    Order = index
                })
                .ToList(),
            JournalEntries = run.JournalEntries
                .Select((entry, index) => new RunJournalEntryEntity
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id.Value,
                    RoomIndex = entry.RoomIndex,
                    RoomDisplayName = entry.RoomDisplayName,
                    Text = entry.Text,
                    Order = index
                })
                .ToList(),
            ActivePalaceLaws = run.ActivePalaceLaws
                .Select(law => new RunActivePalaceLawEntity
                {
                    Id = law.LawId.Value,
                    RunId = run.Id.Value,
                    LawId = law.LawId.Value,
                    Key = law.Key,
                    Name = law.Name,
                    Version = law.Version,
                    Domains = string.Join(",", law.Domains.Select(d => d.ToString())),
                    DisplayName = law.DisplayName,
                    Description = law.Description,
                    Duration = law.Duration,
                    AppliedAtUtc = law.AppliedAtUtc,
                    ExpiresAtRoomId = law.ExpiresAtRoomId,
                    ConsumedAtUtc = law.ConsumedAtUtc,
                    Rarity = law.Rarity,
                    Polarity = law.Polarity,
                    IsMajeure = law.IsMajeure,
                    RoomKey = law.RoomKey,
                    IsCumulExempt = law.IsCumulExempt
                })
                .ToList(),
            ActiveCurses = run.ActiveCurse is not null
                ? [ToActiveCurseEntity(run.ActiveCurse, run.Id.Value)]
                : [],
            ActiveCombat = run.ActiveTacticalCombat is not null
                ? TacticalCombatPersistenceMapper.ToEntity(run.ActiveTacticalCombat, run.Id.Value)
                : null,
            PlayerState = PlayerRuntimeStatePersistenceMapper.ToEntity(run.PlayerState, run.Id.Value),
            InventoryItems = run.PersistedRunItems.Select(item => new RunItemEntity
            {
                Id = item.Id.Value,
                RunId = run.Id.Value,
                DefinitionKey = item.DefinitionKey,
                DefinitionVersion = item.DefinitionVersion,
                DisplayName = item.DisplayName,
                Description = item.Description,
                NarrativeText = item.NarrativeText,
                Type = item.Type.ToString(),
                Rarity = item.Rarity.ToString(),
                Category = item.Category,
                Quantity = item.Quantity,
                MaxStack = item.MaxStack,
                UsageMode = item.UsageMode,
                Lifecycle = item.Lifecycle,
                EffectType = item.EffectType.ToString(),
                EffectAmount = item.EffectAmount,
                EffectSummary = item.EffectSummary,
                IsUsableInCombat = item.IsUsableInCombat,
                IsUsableOutsideCombat = item.IsUsableOutsideCombat,
                SourceRewardOptionId = item.SourceRewardOptionId,
                CreatedAtUtc = item.CreatedAtUtc,
                IsContainer = item.IsContainer,
                ContainerCapacity = item.ContainerCapacity,
                IsLiquid = item.IsLiquid,
                ContainedLiquidDefinitionKey = item.ContainedLiquidDefinitionKey,
                TacticalRange = item.TacticalRange,
                TacticalAreaShape = item.TacticalAreaShape,
                RequiresLineOfSight = item.RequiresLineOfSight,
                GroundRoomId = item.GroundRoomId,
                GroundX = item.GroundX,
                GroundY = item.GroundY
            }).ToList(),
            RunModifiers = run.RunModifiers.Select(m => new RunModifierEntity
            {
                Id = m.Id.Value,
                RunId = run.Id.Value,
                Type = m.Type.ToString(),
                Value = m.Value,
                Duration = m.Duration.ToString(),
                SourceType = m.SourceType,
                SourceKey = m.SourceKey,
                CreatedAtUtc = m.CreatedAtUtc,
                ConsumedAtUtc = m.ConsumedAtUtc,
                ValueMode = m.ValueMode,
                StackPolicy = m.StackPolicy,
                ExpiresAtRoomId = m.ExpiresAtRoomId,
                ExpiresAtCombatId = m.ExpiresAtCombatId
            }).ToList(),
            PlayerSnapshot = run.PlayerSnapshot is not null
                ? ToPlayerSnapshotEntity(run.PlayerSnapshot, run.Id.Value)
                : null
        };

        var snapshot = run.SnapshotData;
        if (snapshot is not null)
        {
            entity.SnapshotCurrentHp = snapshot.CurrentHp;
            entity.SnapshotAttack = snapshot.Attack;
            entity.SnapshotDefense = snapshot.Defense;
            entity.SnapshotSpeed = snapshot.Speed;
            entity.SnapshotMemoryFragments = JsonSerializer.Serialize(snapshot.MemoryFragments);
            entity.SnapshotActivePalaceLaws = JsonSerializer.Serialize(
                snapshot.ActivePalaceLaws.Select(l => new { l.LawId.Value, l.Key, l.Name, l.Version, Domains = l.Domains.Select(d => d.ToString()) }));
            entity.SnapshotRunItemIds = snapshot.RunItemIds is not null
                ? JsonSerializer.Serialize(snapshot.RunItemIds)
                : null;
            entity.SnapshotRunModifierIds = snapshot.RunModifierIds is not null
                ? JsonSerializer.Serialize(snapshot.RunModifierIds)
                : null;
        }

        return entity;
    }

    public static RoomEntity ToEntity(Room room, Guid runId)
    {
        return new RoomEntity
        {
            Id = room.Id.Value,
            RunId = runId,
            Depth = room.Depth,
            RoomType = room.RoomType.ToString(),
            PalaceState = room.PalaceState.ToString(),
            Theme = room.Theme,
            BossId = room.BossProfile?.BossId,
            BossName = room.BossProfile?.Name,
            BossRoomType = room.BossProfile?.RoomType.ToString(),
            BossDangerHint = room.BossProfile?.DangerHint,
            BossEnemyTemplateKey = room.BossProfile?.EnemyTemplateKey,
            State = room.State.ToString(),
            CurrentNodeDepth = room.CurrentNodeDepth,
            MaxNodeDepth = room.MaxNodeDepth,
            LayoutTemplateKey = room.LayoutTemplateKey,
            LayoutTemplateVersion = room.LayoutTemplateVersion,
            CatalogRoomKey = room.CatalogBinding?.Key,
            CatalogDisplayName = room.CatalogBinding?.DisplayName,
            CatalogNarrativeText = room.CatalogBinding?.NarrativeText,
            EnemyPoolKey = room.CatalogBinding?.EnemyPoolKey,
            RewardPoolKey = room.CatalogBinding?.RewardPoolKey,
            LawPoolKey = room.CatalogBinding?.LawPoolKey,
            CursePoolKey = room.CatalogBinding?.CursePoolKey,
            CatalogIsUnique = room.CatalogBinding?.IsUnique ?? false,
            GridWidth = room.Grid.Width,
            GridHeight = room.Grid.Height,
            GridMovementBudget = room.Grid.MovementBudget,
            GridMovementBudgetRemaining = room.Grid.MovementBudgetRemaining,
            GridStartX = room.Grid.StartX,
            GridStartY = room.Grid.StartY,
            GridPartyX = room.Grid.PartyX,
            GridPartyY = room.Grid.PartyY,
            GridRevealedNodeIdsCsv = string.Join(";", room.Grid.RevealedNodeIds.Select(id => id.Value.ToString())),
            GridRevealedCellsCsv = string.Join(";", room.Grid.RevealedCells.Select(cell => $"{cell.X},{cell.Y}")),
            GridElevationCsv = string.Join(",", room.Grid.Elevation),
            GridObstacleCellsCsv = string.Join(";", room.Grid.Obstacles.Select(cell => $"{cell.X},{cell.Y}")),
            GridFloorCellsCsv = string.Join(",", room.Grid.FloorMask.Select(cell => cell ? "1" : "0")),
            GridDoorCellsCsv = string.Join(";", room.Grid.Doors.Select(cell => $"{cell.X},{cell.Y}")),
            GridSurfaceOverridesCsv = string.Join(";", room.Grid.SurfaceOverrides.Select(kv => $"{kv.Key.X},{kv.Key.Y},{kv.Value}")),
            GridDecorPlacementsCsv = string.Join(";", room.Grid.DecorPlacements.Select(kv => $"{kv.Key.X},{kv.Key.Y},{kv.Value}")),
            CurrentGridNodeId = room.CurrentGridNodeId?.Value,
            Nodes = room.Nodes.Select(node => ToEntity(node, room.Id.Value)).ToList(),
            RoomNpcs = room.RoomNpcs.Select(npc => ToEntity(npc, room.Id.Value)).ToList(),
            LocalRuleStates = room.LocalRuleStates.Select(rs => ToEntity(rs, room.Id.Value)).ToList()
        };
    }

    public static LocalRuleStateEntity ToEntity(LocalRuleState state, Guid roomId)
    {
        return new LocalRuleStateEntity
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            LocalRuleKey = state.LocalRuleKey,
            CumulativeSeverity = state.CumulativeSeverity,
            HasBeenInformed = state.HasBeenInformed,
            TriggeredThresholdsCsv = string.Join(",", state.TriggeredThresholds)
        };
    }

    public static RoomNpcEntity ToEntity(RoomNpc npc, Guid roomId)
    {
        return new RoomNpcEntity
        {
            Id = npc.Id.Value,
            RoomId = roomId,
            CatalogNpcKey = npc.CatalogNpcKey,
            OriginX = npc.OriginX,
            OriginY = npc.OriginY,
            X = npc.X,
            Y = npc.Y,
            Behavior = npc.Behavior.ToString(),
            Awareness = npc.Awareness.ToString(),
            AwarenessRadius = npc.AwarenessRadius,
            WaypointsCsv = string.Join(";", npc.Waypoints.Select(cell => $"{cell.X},{cell.Y}")),
            WaypointIndex = npc.WaypointIndex,
            StepCount = npc.StepCount
        };
    }

    public static MapNodeEntity ToEntity(MapNode node, Guid roomId)
    {
        return new MapNodeEntity
        {
            Id = node.Id.Value,
            RoomId = roomId,
            EventType = node.EventType.ToString(),
            Row = node.Row,
            Lane = node.Lane,
            RiskLevel = node.RiskLevel,
            CombatRiskTier = node.CombatRiskTier?.ToString(),
            RewardProfile = node.RewardProfile,
            IsBoss = node.IsBoss,
            State = node.State.ToString(),
            ChosenEventOptionId = node.ChosenEventOptionId,
            HiddenState = node.HiddenState.ToString(),
            DangerTell = node.DangerTell.ToString(),
            ContactBehavior = node.ContactBehavior.ToString(),
            ExitDestinationRoomKey = node.ExitDestinationRoomKey,
            ExitDestinationDisplayName = node.ExitDestinationDisplayName,
            ParentNodeLinks = node.ParentNodeIds
                .Select(parentId => new MapNodeParentNodeEntity
                {
                    MapNodeId = node.Id.Value,
                    ParentNodeId = parentId.Value
                })
                .ToList()
        };
    }

    public static RunPlayerSnapshotEntity ToPlayerSnapshotEntity(RunPlayerSnapshot snapshot, Guid runId)
    {
        var entity = new RunPlayerSnapshotEntity
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            PlayerId = snapshot.PlayerId,
            DisplayName = snapshot.DisplayName,
            CreatedAtUtc = snapshot.CreatedAtUtc.UtcDateTime,
       