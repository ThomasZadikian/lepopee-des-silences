using System.Text.Json;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Persistence.Entities;

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
            StartedAtUtc = run.StartedAt.UtcDateTime,
            EndedAtUtc = run.EndedAt?.UtcDateTime,
            SavedAtUtc = run.SavedAt?.UtcDateTime,
            PreSuspendStatus = run.PreSuspendStatus?.ToString(),
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
            ActivePalaceLaws = run.ActivePalaceLaws
                .Select(law => new RunActivePalaceLawEntity
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id.Value,
                    LawId = law.LawId.Value,
                    Key = law.Key,
                    Name = law.Name,
                    Version = law.Version,
                    Domains = string.Join(",", law.Domains.Select(d => d.ToString()))
                })
                .ToList(),
            ActiveCombat = run.ActiveCombat is not null
                ? CombatPersistenceMapper.ToEntity(run.ActiveCombat, run.Id.Value)
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
            RewardProfile = node.RewardProfile,
            IsBoss = node.IsBoss,
            State = node.State.ToString(),
            ChosenEventOptionId = node.ChosenEventOptionId,
            ParentNodeLinks = node.ParentNodeIds
                .Select(parentId => new MapNodeParentNodeEntity
                {
                    MapNodeId = node.Id.Value,
                    ParentNodeId = parentId.Value
                })
                .ToList()
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

            snapshot = new Run.RunSnapshotData(
                entity.SnapshotCurrentHp.Value,
                entity.SnapshotAttack ?? 0,
                entity.SnapshotDefense ?? 0,
                entity.SnapshotSpeed ?? 0,
                snapshotMemoryFragments,
                snapshotLaws);
        }

        var activeCombat = entity.ActiveCombat is not null
            ? CombatPersistenceMapper.ToDomain(entity.ActiveCombat)
            : null;

        return Run.Rehydrate(
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
            new DateTimeOffset(entity.StartedAtUtc, TimeSpan.Zero),
            entity.EndedAtUtc.HasValue ? new DateTimeOffset(entity.EndedAtUtc.Value, TimeSpan.Zero) : null,
            entity.SavedAtUtc.HasValue ? new DateTimeOffset(entity.SavedAtUtc.Value, TimeSpan.Zero) : null,
            entity.CurrentRoomIndex,
            rooms,
            memoryFragments,
            activePalaceLaws,
            entity.PreSuspendStatus is not null ? Enum.Parse<RunStatus>(entity.PreSuspendStatus) : null,
            snapshot,
            activeCombat);
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

        return Room.Rehydrate(
            new RoomId(entity.Id),
            entity.Depth,
            Enum.Parse<RoomType>(entity.RoomType),
            entity.Theme,
            bossProfile,
            Enum.Parse<RoomState>(entity.State),
            entity.CurrentNodeDepth,
            nodes,
            entity.LayoutTemplateKey,
            entity.LayoutTemplateVersion);
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
            entity.ChosenEventOptionId);
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
            domains);
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

    private sealed record SnapshotLawDto(
        string Value,
        string Key,
        string Name,
        string Version,
        string[] Domains);
}
