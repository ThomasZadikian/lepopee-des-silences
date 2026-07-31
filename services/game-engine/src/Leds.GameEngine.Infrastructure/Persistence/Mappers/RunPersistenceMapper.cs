using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;
using Leds.GameEngine.Domain.Npcs;

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
            Seed = run.Seed,
            GeneratorVersion = run.GeneratorVersion,
            MarkovMatrixVersion = run.MarkovMatrixVersion,
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
                EffectSetKey = item.EffectSetKey,
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
            BossId = room.BossProfile.BossId,
            BossName = room.BossProfile.Name,
            BossRoomType = room.BossProfile.RoomType.ToString(),
            BossDangerHint = room.BossProfile.DangerHint,
            BossEnemyTemplateKey = room.BossProfile.EnemyTemplateKey,
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
            CurrentGridNodeId = room.CurrentGridNodeId?.Value,
            Nodes = room.Nodes.Select(node => ToEntity(node, room.Id.Value)).ToList()
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
            Characters = snapshot.Characters
                .Select((c, index) => ToCharacterSnapshotEntity(c, index))
                .ToList()
        };

        return entity;
    }

    public static RunCharacterSnapshotEntity ToCharacterSnapshotEntity(RunCharacterSnapshot snapshot, int order)
    {
        var entityId = Guid.NewGuid();

        var entity = new RunCharacterSnapshotEntity
        {
            Id = entityId,
            CharacterId = snapshot.CharacterId,
            DefinitionKey = snapshot.DefinitionKey,
            DisplayName = snapshot.DisplayName,
            SnapshotOrder = order,
            EquippedItemKeysCsv = string.Join(';', snapshot.EquippedItemKeys),
            StatBlock = snapshot.StatBlock is not null
                ? ToStatSnapshotEntity(snapshot.StatBlock, entityId)
                : null,
            Skills = snapshot.Skills.Select(s => ToSkillSnapshotEntity(s, entityId)).ToList()
        };

        return entity;
    }

    public static RunCharacterStatSnapshotEntity ToStatSnapshotEntity(RunCharacterStatSnapshot stat, Guid characterSnapshotId)
    {
        return new RunCharacterStatSnapshotEntity
        {
            Id = Guid.NewGuid(),
            CharacterSnapshotId = characterSnapshotId,
            MaxVitality = stat.MaxVitality,
            AttackPower = stat.AttackPower,
            Defense = stat.Defense,
            StartingGuard = stat.StartingGuard,
            Speed = stat.Speed,
            Initiative = stat.Initiative,
            Focus = stat.Focus,
            Mana = stat.Mana,
            Charge = stat.Charge,
            MagicAttack = stat.MagicAttack,
            MagicDefense = stat.MagicDefense,
            Movement = stat.Movement
        };
    }

    public static RunCharacterSkillSnapshotEntity ToSkillSnapshotEntity(RunCharacterSkillSnapshot skill, Guid characterSnapshotId)
    {
        return new RunCharacterSkillSnapshotEntity
        {
            Id = Guid.NewGuid(),
            CharacterSnapshotId = characterSnapshotId,
            SkillDefinitionKey = skill.SkillDefinitionKey,
            DisplayName = skill.DisplayName,
            SkillType = skill.SkillType,
            TargetingMode = skill.TargetingMode,
            EffectType = skill.EffectType,
            ManaCost = skill.ManaCost,
            ChargeCost = skill.ChargeCost,
            BasePower = skill.BasePower,
            Category = skill.Category,
            BasePowerIsPercentOfMaxVitality = skill.BasePowerIsPercentOfMaxVitality,
            TacticalRange = skill.TacticalRange,
            TacticalAreaShape = skill.TacticalAreaShape,
            RequiresLineOfSight = skill.RequiresLineOfSight,
            Cooldown = skill.Cooldown,
            IsUltimate = skill.IsUltimate,
            EmotionalRegister = skill.EmotionalRegister,
            TemporarySlot = skill.TemporarySlot
        };
    }

    // -----------------------------------------------------------------------
    // Entity → Domain
    // -----------------------------------------------------------------------

    public static Run ToDomain(RunEntity entity)
    {
        var rooms = entity.Rooms.Select(ToDomain).ToList();
        var memoryFragments = entity.MemoryFragments
            .OrderBy(f => f.Order)
            .Select(f => f.FragmentKey)
            .ToList();
        var journalEntries = entity.JournalEntries
            .OrderBy(e => e.Order)
            .Select(e => new RunJournalEntry(e.RoomIndex, e.RoomDisplayName, e.Text))
            .ToList();
        var activePalaceLaws = entity.ActivePalaceLaws
            .Select(ToDomain)
            .ToList();

        Run.RunSnapshotData? snapshot = null;
        if (entity.SnapshotCurrentHp.HasValue)
        {
            var snapshotMemoryFragments = string.IsNullOrEmpty(entity.SnapshotMemoryFragments)
                ? []
                : JsonSerializer.Deserialize<string[]>(entity.SnapshotMemoryFragments) ?? [];

            var snapshotLaws = string.IsNullOrEmpty(entity.SnapshotActivePalaceLaws)
                ? []
                : DeserializeSnapshotLaws(entity.SnapshotActivePalaceLaws);

            var snapshotRunItemIds = string.IsNullOrEmpty(entity.SnapshotRunItemIds)
                ? null
                : JsonSerializer.Deserialize<Guid[]>(entity.SnapshotRunItemIds);

            var snapshotRunModifierIds = string.IsNullOrEmpty(entity.SnapshotRunModifierIds)
                ? null
                : JsonSerializer.Deserialize<Guid[]>(entity.SnapshotRunModifierIds);

            snapshot = new Run.RunSnapshotData(
                entity.SnapshotCurrentHp.Value,
                entity.SnapshotAttack ?? 0,
                entity.SnapshotDefense ?? 0,
                entity.SnapshotSpeed ?? 0,
                snapshotMemoryFragments,
                snapshotLaws,
                snapshotRunItemIds,
                snapshotRunModifierIds);
        }

        var activeTacticalCombat = entity.ActiveCombat is not null
            ? TacticalCombatPersistenceMapper.ToDomain(entity.ActiveCombat)
            : null;

        var playerState = entity.PlayerState is not null
            ? PlayerRuntimeStatePersistenceMapper.ToDomain(entity.PlayerState)
            : null;

        var runItems = entity.InventoryItems.Select(item => RunItem.Rehydrate(
            new RunItemId(item.Id),
            item.DefinitionKey,
            item.DisplayName,
            item.Description,
            Enum.Parse<RunItemType>(item.Type),
            Enum.Parse<RunItemRarity>(item.Rarity),
            item.Quantity,
            Enum.Parse<RunItemEffectType>(item.EffectType),
            item.EffectAmount,
            item.CreatedAtUtc,
            definitionVersion: item.DefinitionVersion,
            narrativeText: item.NarrativeText,
            category: item.Category,
            usageMode: item.UsageMode,
            lifecycle: item.Lifecycle,
            maxStack: item.MaxStack,
            effectSetKey: item.EffectSetKey,
            effectSummary: item.EffectSummary,
            isUsableInCombat: item.IsUsableInCombat,
            isUsableOutsideCombat: item.IsUsableOutsideCombat,
            sourceRewardOptionId: item.SourceRewardOptionId,
            isContainer: item.IsContainer,
            containerCapacity: item.ContainerCapacity,
            isLiquid: item.IsLiquid,
            containedLiquidDefinitionKey: item.ContainedLiquidDefinitionKey,
            tacticalRange: item.TacticalRange,
            tacticalAreaShape: item.TacticalAreaShape,
            requiresLineOfSight: item.RequiresLineOfSight,
            groundRoomId: item.GroundRoomId,
            groundX: item.GroundX,
            groundY: item.GroundY)).ToList();

        var runModifiers = entity.RunModifiers.Select(m => RunModifier.Rehydrate(
            new RunModifierId(m.Id),
            Enum.Parse<RunModifierType>(m.Type),
            m.Value,
            Enum.Parse<RunModifierDuration>(m.Duration),
            m.SourceType,
            m.SourceKey,
            m.CreatedAtUtc,
            m.ConsumedAtUtc,
            m.ValueMode ?? "Flat",
            m.StackPolicy ?? "Additive",
            m.ExpiresAtRoomId,
            m.ExpiresAtCombatId)).ToList();

        var playerSnapshot = entity.PlayerSnapshot is not null
            ? ToDomainPlayerSnapshot(entity.PlayerSnapshot)
            : null;

       var run = Run.Rehydrate(
            new RunId(entity.Id),
            entity.PlayerId,
            entity.Seed,
            entity.GeneratorVersion,
            entity.MarkovMatrixVersion,
            Enum.Parse<RunStatus>(entity.Status),
            new RoomId(entity.CurrentRoomId),
            entity.ActiveCombatId.HasValue ? new CombatId(entity.ActiveCombatId.Value) : null,
            entity.PendingRewardOfferId.HasValue ? new RewardOfferId(entity.PendingRewardOfferId.Value) : null,
            entity.MaxHp,
            entity.CurrentHp,
            entity.Attack,
            entity.Defense,
            entity.Speed,
            entity.Focus,
            new DateTimeOffset(entity.StartedAtUtc, TimeSpan.Zero),
            entity.EndedAtUtc.HasValue ? new DateTimeOffset(entity.EndedAtUtc.Value, TimeSpan.Zero) : null,
            entity.SavedAtUtc.HasValue ? new DateTimeOffset(entity.SavedAtUtc.Value, TimeSpan.Zero) : null,
            entity.CurrentRoomIndex,
            rooms,
            memoryFragments,
            activePalaceLaws,
            entity.PreSuspendStatus is not null ? Enum.Parse<RunStatus>(entity.PreSuspendStatus) : null,
            snapshot,
            playerState,
            runItems,
            runModifiers,
            playerSnapshot: playerSnapshot,
            activeCurse: entity.ActiveCurses.FirstOrDefault() is { } curseEntity ? ToDomain(curseEntity) : null,
            runItemCapacity: entity.RunItemCapacity > 0 ? entity.RunItemCapacity : Run.DefaultRunItemCapacity,
            typedDamageReductions: string.IsNullOrWhiteSpace(entity.TypedDamageReductionsJson)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, int>>(entity.TypedDamageReductionsJson),
            hitChanceBonusPercent: entity.HitChanceBonusPercent,
            dotDurationReductionPercent: entity.DotDurationReductionPercent,
            dotDamageReductionPercent: entity.DotDamageReductionPercent,
            magicDamageBonusPercent: entity.MagicDamageBonusPercent,
            magicDamageReductionPercent: entity.MagicDamageReductionPercent,
            criticalChanceBonusPercent: entity.CriticalChanceBonusPercent,
            guardBonusPercent: entity.GuardBonusPercent,
            dotDamageBonusPercent: entity.DotDamageBonusPercent,
            journalEnabled: entity.JournalEnabled,
            journalEntries: journalEntries,
            lawDenialEnabled: entity.LawDenialEnabled,
            lawDenialLastUsedRoomIndex: entity.LawDenialLastUsedRoomIndex,
            lastPromulgationFloorIndex: entity.LastPromulgationFloorIndex,
            forgottenSkillKey: entity.ForgottenSkillKey,
            activeTacticalCombat: activeTacticalCombat,
            suspendedSevereLawModifierIds: string.IsNullOrWhiteSpace(entity.SuspendedSevereLawModifierIdsJson)
                ? null
                : JsonSerializer.Deserialize<Guid[]>(entity.SuspendedSevereLawModifierIdsJson),
            reputationGainBonusPercent: entity.ReputationGainBonusPercent,
            himLitProtectionEnabled: entity.HimLitProtectionEnabled,
            healingBonusPercent: entity.HealingBonusPercent,
            caliceInfiniEnabled: entity.CaliceInfiniEnabled,
            caliceInfiniLastUsedRoomIndex: entity.CaliceInfiniLastUsedRoomIndex,
            magicAttack: entity.MagicAttack,
            magicDefense: entity.MagicDefense);

        RehydrateNpcEncounters(run, entity);
        return run;
    }

    public static Room ToDomain(RoomEntity entity)
    {
        var nodes = entity.Nodes.Select(ToDomain).ToList();

        var bossProfile = RoomBossProfile.Create(
            entity.BossId,
            entity.BossName,
            Enum.Parse<RoomType>(entity.BossRoomType),
            entity.BossDangerHint,
            entity.BossEnemyTemplateKey);

        var grid = RoomGrid.Rehydrate(
            entity.GridWidth,
            entity.GridHeight,
            entity.GridMovementBudget,
            entity.GridMovementBudgetRemaining,
            entity.GridStartX,
            entity.GridStartY,
            entity.GridPartyX,
            entity.GridPartyY,
            ParseGridRevealedNodeIds(entity.GridRevealedNodeIdsCsv),
            ParseGridRevealedCells(entity.GridRevealedCellsCsv),
            ParseGridElevation(entity.GridElevationCsv, entity.GridWidth, entity.GridHeight),
            ParseGridRevealedCells(entity.GridObstacleCellsCsv).ToArray(),
            ParseGridFloorCells(entity.GridFloorCellsCsv, entity.GridWidth, entity.GridHeight));

        var room = Room.Rehydrate(
            new RoomId(entity.Id),
            entity.Depth,
            Enum.Parse<RoomType>(entity.RoomType),
            string.IsNullOrWhiteSpace(entity.PalaceState)
                ? PalaceRoomState.Neutral
                : Enum.Parse<PalaceRoomState>(entity.PalaceState),
            entity.Theme,
            bossProfile,
            Enum.Parse<RoomState>(entity.State),
            entity.CurrentNodeDepth,
            nodes,
            entity.LayoutTemplateKey,
            entity.LayoutTemplateVersion,
            grid,
            entity.CurrentGridNodeId is { } currentGridNodeId ? new NodeId(currentGridNodeId) : null);

        if (!string.IsNullOrWhiteSpace(entity.CatalogRoomKey))
        {
            room.AttachCatalogBinding(new CatalogRoomBinding(
                entity.CatalogRoomKey,
                entity.CatalogDisplayName ?? entity.CatalogRoomKey,
                entity.CatalogNarrativeText,
                entity.EnemyPoolKey,
                entity.RewardPoolKey,
                entity.LawPoolKey,
                entity.CursePoolKey,
                entity.CatalogIsUnique));
        }

        return room;
    }

    private static IEnumerable<NodeId> ParseGridRevealedNodeIds(string? csv)
    {
        if (string.IsNullOrEmpty(csv))
        {
            return [];
        }

        return csv.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => new NodeId(Guid.Parse(id)));
    }

    private static IEnumerable<(int X, int Y)> ParseGridRevealedCells(string? csv)
    {
        if (string.IsNullOrEmpty(csv))
        {
            return [];
        }

        return csv.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair =>
            {
                var parts = pair.Split(',');
                return (X: int.Parse(parts[0]), Y: int.Parse(parts[1]));
            });
    }

    /// <summary>Falls back to a flat (all-zero) map for rooms persisted before this field
    /// existed — matches this project's nullable/defaulted-column migration convention.</summary>
    /// <summary>
    /// Empty means "every cell is floor" — how a room persisted before rooms had a shape must
    /// be read, matching this project's defaulted-column migration convention. A stored mask of
    /// the wrong length is treated the same way rather than crashing a run mid-load.
    /// </summary>
    private static IReadOnlyList<bool> ParseGridFloorCells(string? csv, int width, int height)
    {
        if (string.IsNullOrEmpty(csv))
        {
            return Enumerable.Repeat(true, width * height).ToArray();
        }

        var parsed = csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(cell => cell == "1")
            .ToArray();

        return parsed.Length == width * height
            ? parsed
            : Enumerable.Repeat(true, width * height).ToArray();
    }

    /// <summary>Missing/unknown reads back as the enum's default — the "nothing special" value.</summary>
    private static TEnum ParseEnumOrDefault<TEnum>(string? value) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, out var parsed) ? parsed : default;

    private static IReadOnlyList<int> ParseGridElevation(string? csv, int width, int height)
    {
        if (string.IsNullOrEmpty(csv))
        {
            return new int[width * height];
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToArray();
    }

    public static MapNode ToDomain(MapNodeEntity entity)
    {
        var parentNodeIds = entity.ParentNodeLinks
            .Select(link => new NodeId(link.ParentNodeId))
            .ToList();

        return MapNode.Rehydrate(
            new NodeId(entity.Id),
            Enum.Parse<NodeEventType>(entity.EventType),
            entity.Row,
            entity.Lane,
            entity.RiskLevel,
            entity.RewardProfile,
            parentNodeIds,
            entity.IsBoss,
            Enum.Parse<NodeState>(entity.State),
            entity.ChosenEventOptionId,
            string.IsNullOrEmpty(entity.CombatRiskTier) ? null : Enum.Parse<RiskTier>(entity.CombatRiskTier),
            ParseEnumOrDefault<HiddenState>(entity.HiddenState),
            ParseEnumOrDefault<DangerTell>(entity.DangerTell),
            ParseEnumOrDefault<ContactBehavior>(entity.ContactBehavior));
    }

    public static ActivePalaceLaw ToDomain(RunActivePalaceLawEntity entity)
    {
        var domains = entity.Domains
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => Enum.Parse<PalaceLawDomain>(d.Trim()))
            .ToArray();

        return ActivePalaceLaw.Rehydrate(
            new PalaceLawId(entity.LawId),
            entity.Key,
            entity.Name,
            entity.Version,
            domains,
            entity.DisplayName,
            entity.Description,
            entity.Duration,
            entity.AppliedAtUtc,
            entity.ExpiresAtRoomId,
            entity.ConsumedAtUtc,
            entity.Rarity,
            entity.Polarity,
            entity.IsMajeure,
            entity.RoomKey,
            entity.IsCumulExempt);
    }

    public static ActiveCurse ToDomain(ActiveCurseEntity entity)
    {
        return ActiveCurse.Rehydrate(
            entity.Id,
            entity.Key,
            entity.DisplayName,
            entity.Description,
            entity.DifficultyDelta,
            entity.AppliedAtUtc,
            entity.CurseDefinitionKey,
            entity.Severity,
            entity.Duration,
            entity.ExpiresAtRoomId,
            entity.ConsumedAtUtc,
            entity.EffectSetKey);
    }

    public static ActiveCurseEntity ToActiveCurseEntity(ActiveCurse curse, Guid runId)
    {
        return new ActiveCurseEntity
        {
            Id = curse.Id,
            RunId = runId,
            Key = curse.Key,
            DisplayName = curse.DisplayName,
            Description = curse.Description,
            DifficultyDelta = curse.DifficultyDelta,
            AppliedAtUtc = curse.AppliedAtUtc,
            CurseDefinitionKey = curse.CurseDefinitionKey,
            Severity = curse.Severity,
            Duration = curse.Duration,
            ExpiresAtRoomId = curse.ExpiresAtRoomId,
            ConsumedAtUtc = curse.ConsumedAtUtc,
            EffectSetKey = curse.EffectSetKey
        };
    }

    private static ActivePalaceLaw[] DeserializeSnapshotLaws(string json)
    {
        var raw = JsonSerializer.Deserialize<List<SnapshotLawDto>>(json);
        if (raw is null) return [];

        return raw.Select(l => ActivePalaceLaw.Rehydrate(
            new PalaceLawId(Guid.Parse(l.Value)),
            l.Key,
            l.Name,
            l.Version,
            l.Domains.Select(d => Enum.Parse<PalaceLawDomain>(d.Trim())).ToArray()
        )).ToArray();
    }

    private static RunPlayerSnapshot ToDomainPlayerSnapshot(RunPlayerSnapshotEntity entity)
    {
        var characters = entity.Characters
           .OrderBy(c => c.SnapshotOrder)
           .Select(ToDomainCharacterSnapshot)
           .ToList(); ;

        return RunPlayerSnapshot.Rehydrate(
            entity.Id,
            entity.PlayerId,
            entity.DisplayName,
            new DateTimeOffset(entity.CreatedAtUtc, TimeSpan.Zero),
            characters);
    }

    private static RunCharacterSnapshot ToDomainCharacterSnapshot(RunCharacterSnapshotEntity entity)
    {
        var statBlock = entity.StatBlock is not null
            ? ToDomainCharacterStatSnapshot(entity.StatBlock)
            : null;

        var skills = entity.Skills
            .Select(ToDomainCharacterSkillSnapshot)
            .ToList();

        return RunCharacterSnapshot.Rehydrate(
            entity.Id,
            entity.CharacterId,
            entity.DefinitionKey,
            entity.DisplayName,
            statBlock,
            skills,
            string.IsNullOrWhiteSpace(entity.EquippedItemKeysCsv)
                ? []
                : entity.EquippedItemKeysCsv.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static RunCharacterStatSnapshot ToDomainCharacterStatSnapshot(RunCharacterStatSnapshotEntity entity)
    {
        return RunCharacterStatSnapshot.Rehydrate(
            entity.Id,
            entity.MaxVitality,
            entity.AttackPower,
            entity.Defense,
            entity.StartingGuard,
            entity.Speed,
            entity.Initiative,
            entity.Focus,
            entity.Mana,
            entity.Charge,
            entity.MagicAttack,
            entity.MagicDefense,
            entity.Movement);
    }

    private static RunCharacterSkillSnapshot ToDomainCharacterSkillSnapshot(RunCharacterSkillSnapshotEntity entity)
    {
        return RunCharacterSkillSnapshot.Rehydrate(
            entity.Id,
            entity.SkillDefinitionKey,
            entity.DisplayName,
            entity.SkillType,
            entity.TargetingMode,
            entity.EffectType,
            entity.ManaCost,
            entity.ChargeCost,
            entity.BasePower,
            entity.Category,
            entity.BasePowerIsPercentOfMaxVitality,
            entity.TacticalRange,
            entity.TacticalAreaShape,
            entity.RequiresLineOfSight,
            entity.Cooldown,
            entity.IsUltimate,
            entity.EmotionalRegister,
            entity.TemporarySlot);
    }

    // -----------------------------------------------------------------------
    // RewardOffer / RewardOption mapping
    // -----------------------------------------------------------------------

    public static RewardOfferEntity ToRewardOfferEntity(
        Domain.Rewards.RewardOffer offer, Guid runId)
    {
        return new RewardOfferEntity
        {
            Id = offer.Id.Value,
            RunId = runId,
            Source = offer.Source.ToString(),
            State = offer.State.ToString(),
            DefeatedEnemiesJson = SerializeDefeatedEnemies(offer.DefeatedEnemies),
            CreatedAtUtc = DateTime.UtcNow,
            SelectedAtUtc = null,
            Options = offer.Choices.Select((choice, index) => new RewardOptionEntity
            {
                Id = choice.Id.Value,
                RewardOfferId = offer.Id.Value,
                RewardType = choice.RewardType.ToString(),
                Label = choice.Label,
                Description = choice.Description,
                PayloadKey = choice.PayloadKey,
                SourceEnemyKey = choice.SourceEnemyKey,
                SourceEnemyDisplayName = choice.SourceEnemyDisplayName,
                IsSelected = false,
                SelectionOrder = index
            }).ToList()
        };
    }

    public static Domain.Rewards.RewardOffer ToRewardOfferDomain(RewardOfferEntity entity)
    {
        var choices = entity.Options
            .OrderBy(o => o.SelectionOrder)
            .Select(ToRewardChoiceDomain)
            .ToList();

        var source = Enum.Parse<RewardSource>(entity.Source);
        var state = Enum.Parse<RewardOfferState>(entity.State);
        var selectedChoiceId = entity.Options
            .FirstOrDefault(o => o.IsSelected)
            ?.Id is Guid selectedId
            ? new RewardChoiceId(selectedId)
            : (RewardChoiceId?)null;

        return Domain.Rewards.RewardOffer.Rehydrate(
            new RewardOfferId(entity.Id),
            source,
            choices,
            state,
            selectedChoiceId,
            DeserializeDefeatedEnemies(entity.DefeatedEnemiesJson));
    }

    private sealed record DefeatedEnemyLootEntrySnapshot(
        string ItemKey,
        string ItemDisplayName,
        string Rarity,
        int DropPercent);

    private sealed record DefeatedEnemySummarySnapshot(
        string EnemyKey,
        string DisplayName,
        string Description,
        int Count,
        List<DefeatedEnemyLootEntrySnapshot> LootEntries);

    private static string? SerializeDefeatedEnemies(IReadOnlyCollection<Domain.Rewards.DefeatedEnemySummary> defeatedEnemies)
    {
        if (defeatedEnemies.Count == 0)
        {
            return null;
        }

        var snapshots = defeatedEnemies.Select(e => new DefeatedEnemySummarySnapshot(
            e.EnemyKey,
            e.DisplayName,
            e.Description,
            e.Count,
            e.LootEntries.Select(l => new DefeatedEnemyLootEntrySnapshot(l.ItemKey, l.ItemDisplayName, l.Rarity, l.DropPercent)).ToList())).ToList();

        return JsonSerializer.Serialize(snapshots);
    }

    private static List<Domain.Rewards.DefeatedEnemySummary> DeserializeDefeatedEnemies(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var snapshots = JsonSerializer.Deserialize<List<DefeatedEnemySummarySnapshot>>(json) ?? [];

        return snapshots.Select(s => new Domain.Rewards.DefeatedEnemySummary(
            s.EnemyKey,
            s.DisplayName,
            s.Description,
            s.Count,
            s.LootEntries.Select(l => new Domain.Rewards.DefeatedEnemyLootEntry(l.ItemKey, l.ItemDisplayName, l.Rarity, l.DropPercent)).ToList())).ToList();
    }

    public static RewardChoice ToRewardChoiceDomain(RewardOptionEntity entity)
    {
        return RewardChoice.Rehydrate(
            new RewardChoiceId(entity.Id),
            Enum.Parse<RewardType>(entity.RewardType),
            entity.Label,
            entity.Description,
            entity.PayloadKey ?? string.Empty,
            entity.SourceEnemyKey,
            entity.SourceEnemyDisplayName);
    }
    private static readonly JsonSerializerOptions NpcJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record NpcRelationshipSnapshot(
        string NpcKey,
        int RelationshipScore,
        Dictionary<string, WoundState> WoundStates,
        List<string> Flags,
        int TimesMet,
        string? CurrentDialogueNodeKey);

    private static string SerializeNpcRelationships(IReadOnlyCollection<NpcRelationship> relationships)
    {
        var snapshots = relationships.Select(r => new NpcRelationshipSnapshot(
            r.NpcKey,
            r.RelationshipScore,
            r.WoundStates.ToDictionary(kv => kv.Key, kv => kv.Value),
            r.Flags.ToList(),
            r.TimesMet,
            r.CurrentDialogueNodeKey)).ToList();

        return JsonSerializer.Serialize(snapshots, NpcJsonOptions);
    }

    private static void RehydrateNpcEncounters(Run run, RunEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.NpcRelationshipsJson))
        {
            var snapshots = JsonSerializer.Deserialize<List<NpcRelationshipSnapshot>>(
                entity.NpcRelationshipsJson, NpcJsonOptions) ?? [];

            foreach (var snapshot in snapshots)
            {
                run.RehydrateNpcRelationship(NpcRelationship.Rehydrate(
                    snapshot.NpcKey,
                    snapshot.RelationshipScore,
                    snapshot.WoundStates,
                    snapshot.Flags,
                    snapshot.TimesMet,
                    snapshot.CurrentDialogueNodeKey));
            }
        }

        run.RehydrateActiveNpcKey(entity.ActiveNpcKey);
    }

    private sealed record SnapshotLawDto(
        string Value,
        string Key,
        string Name,
        string Version,
        string[] Domains);

}
