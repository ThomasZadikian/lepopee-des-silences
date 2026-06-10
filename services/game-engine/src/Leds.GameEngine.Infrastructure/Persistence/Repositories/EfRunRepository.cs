using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Leds.GameEngine.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.Infrastructure.Persistence.Repositories;

public sealed class EfRunRepository : IRunRepository
{
    private readonly GameEngineDbContext _dbContext;

    public EfRunRepository(GameEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Run?> GetByIdAsync(RunId runId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Runs
            .Include(run => run.Rooms)
                .ThenInclude(room => room.Nodes)
                    .ThenInclude(node => node.ParentNodeLinks)
            .Include(run => run.MemoryFragments)
            .Include(run => run.ActivePalaceLaws)
            .Include(run => run.ActiveCombat)
                .ThenInclude(combat => combat!.Combatants)
                    .ThenInclude(combatant => combatant.Skills)
            .FirstOrDefaultAsync(run => run.Id == runId.Value, cancellationToken);

        return entity is null ? null : RunPersistenceMapper.ToDomain(entity);
    }

    public async Task AddAsync(Run run, CancellationToken cancellationToken)
    {
        var entity = RunPersistenceMapper.ToEntity(run);
        _dbContext.Runs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Run run, CancellationToken cancellationToken)
    {
        var runId = run.Id.Value;

        var existing = await _dbContext.Runs
            .Include(r => r.Rooms)
                .ThenInclude(room => room.Nodes)
                    .ThenInclude(node => node.ParentNodeLinks)
            .Include(r => r.MemoryFragments)
            .Include(r => r.ActivePalaceLaws)
            .Include(r => r.ActiveCombat)
                .ThenInclude(combat => combat!.Combatants)
                    .ThenInclude(combatant => combatant.Skills)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException($"Run with id '{runId}' was not found for update.");
        }

        var entity = RunPersistenceMapper.ToEntity(run);

        existing.PlayerId = entity.PlayerId;
        existing.Status = entity.Status;
        existing.Seed = entity.Seed;
        existing.GeneratorVersion = entity.GeneratorVersion;
        existing.MarkovMatrixVersion = entity.MarkovMatrixVersion;
        existing.CurrentRoomId = entity.CurrentRoomId;
        existing.CurrentRoomIndex = entity.CurrentRoomIndex;
        existing.ActiveCombatId = entity.ActiveCombatId;
        existing.PendingRewardOfferId = entity.PendingRewardOfferId;
        existing.MaxHp = entity.MaxHp;
        existing.CurrentHp = entity.CurrentHp;
        existing.Attack = entity.Attack;
        existing.Defense = entity.Defense;
        existing.Speed = entity.Speed;
        existing.StartedAtUtc = entity.StartedAtUtc;
        existing.EndedAtUtc = entity.EndedAtUtc;
        existing.SavedAtUtc = entity.SavedAtUtc;
        existing.PreSuspendStatus = entity.PreSuspendStatus;
        existing.SnapshotCurrentHp = entity.SnapshotCurrentHp;
        existing.SnapshotAttack = entity.SnapshotAttack;
        existing.SnapshotDefense = entity.SnapshotDefense;
        existing.SnapshotSpeed = entity.SnapshotSpeed;
        existing.SnapshotMemoryFragments = entity.SnapshotMemoryFragments;
        existing.SnapshotActivePalaceLaws = entity.SnapshotActivePalaceLaws;
        existing.UpdatedAtUtc = entity.UpdatedAtUtc;

        UpdateRooms(existing, entity);
        UpdateMemoryFragments(existing, entity);
        UpdateActivePalaceLaws(existing, entity);

        CombatEntity? incomingCombat = run.ActiveCombat is not null
            ? CombatPersistenceMapper.ToEntity(run.ActiveCombat, runId)
            : null;

        UpdateActiveCombat(existing, incomingCombat);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void UpdateRooms(RunEntity existing, RunEntity incoming)
    {
        var existingRoomIds = existing.Rooms.Select(r => r.Id).ToHashSet();
        var incomingRoomIds = incoming.Rooms.Select(r => r.Id).ToHashSet();

        var roomsToRemove = existing.Rooms
            .Where(r => !incomingRoomIds.Contains(r.Id))
            .ToList();

        foreach (var room in roomsToRemove)
        {
            existing.Rooms.Remove(room);
        }

        foreach (var incomingRoom in incoming.Rooms)
        {
            var existingRoom = existing.Rooms.FirstOrDefault(r => r.Id == incomingRoom.Id);

            if (existingRoom is null)
            {
                existing.Rooms.Add(incomingRoom);
                continue;
            }

            existingRoom.Depth = incomingRoom.Depth;
            existingRoom.RoomType = incomingRoom.RoomType;
            existingRoom.Theme = incomingRoom.Theme;
            existingRoom.BossId = incomingRoom.BossId;
            existingRoom.BossName = incomingRoom.BossName;
            existingRoom.BossRoomType = incomingRoom.BossRoomType;
            existingRoom.BossDangerHint = incomingRoom.BossDangerHint;
            existingRoom.BossEnemyTemplateKey = incomingRoom.BossEnemyTemplateKey;
            existingRoom.State = incomingRoom.State;
            existingRoom.CurrentNodeDepth = incomingRoom.CurrentNodeDepth;
            existingRoom.MaxNodeDepth = incomingRoom.MaxNodeDepth;
            existingRoom.LayoutTemplateKey = incomingRoom.LayoutTemplateKey;
            existingRoom.LayoutTemplateVersion = incomingRoom.LayoutTemplateVersion;

            UpdateNodes(existingRoom, incomingRoom);
        }
    }

    private void UpdateNodes(RoomEntity existingRoom, RoomEntity incomingRoom)
    {
        var existingNodeIds = existingRoom.Nodes.Select(n => n.Id).ToHashSet();
        var incomingNodeIds = incomingRoom.Nodes.Select(n => n.Id).ToHashSet();

        var nodesToRemove = existingRoom.Nodes
            .Where(n => !incomingNodeIds.Contains(n.Id))
            .ToList();

        foreach (var node in nodesToRemove)
        {
            existingRoom.Nodes.Remove(node);
        }

        foreach (var incomingNode in incomingRoom.Nodes)
        {
            var existingNode = existingRoom.Nodes.FirstOrDefault(n => n.Id == incomingNode.Id);

            if (existingNode is null)
            {
                existingRoom.Nodes.Add(incomingNode);
                continue;
            }

            existingNode.EventType = incomingNode.EventType;
            existingNode.Row = incomingNode.Row;
            existingNode.Lane = incomingNode.Lane;
            existingNode.RiskLevel = incomingNode.RiskLevel;
            existingNode.RewardProfile = incomingNode.RewardProfile;
            existingNode.IsBoss = incomingNode.IsBoss;
            existingNode.State = incomingNode.State;
            existingNode.ChosenEventOptionId = incomingNode.ChosenEventOptionId;

            UpdateParentNodeLinks(existingNode, incomingNode);
        }
    }

    private void UpdateParentNodeLinks(MapNodeEntity existingNode, MapNodeEntity incomingNode)
    {
        existingNode.ParentNodeLinks.Clear();
        existingNode.ParentNodeLinks.AddRange(incomingNode.ParentNodeLinks);
    }

    private void UpdateMemoryFragments(RunEntity existing, RunEntity incoming)
    {
        existing.MemoryFragments.Clear();
        existing.MemoryFragments.AddRange(incoming.MemoryFragments);
    }

    private void UpdateActivePalaceLaws(RunEntity existing, RunEntity incoming)
    {
        existing.ActivePalaceLaws.Clear();
        existing.ActivePalaceLaws.AddRange(incoming.ActivePalaceLaws);
    }

    private void UpdateActiveCombat(RunEntity existingRun, CombatEntity? incomingCombat)
    {
        var existingCombat = existingRun.ActiveCombat;

        if (incomingCombat is null)
        {
            if (existingCombat is not null)
            {
                foreach (var combatant in existingCombat.Combatants)
                {
                    _dbContext.CombatantSkills.RemoveRange(combatant.Skills);
                }
                _dbContext.Combatants.RemoveRange(existingCombat.Combatants);
                _dbContext.Combats.Remove(existingCombat);
                existingRun.ActiveCombat = null;
                existingRun.ActiveCombatId = null;
            }

            return;
        }

        if (existingCombat is null)
        {
            existingRun.ActiveCombat = incomingCombat;
            existingRun.ActiveCombatId = incomingCombat.Id;
            return;
        }

        existingCombat.Status = incomingCombat.Status;
        existingCombat.TurnNumber = incomingCombat.TurnNumber;
        existingCombat.ActiveCombatantId = incomingCombat.ActiveCombatantId;
        existingCombat.UpdatedAtUtc = incomingCombat.UpdatedAtUtc;

        UpdateCombatants(existingCombat, incomingCombat);
    }

    private void UpdateCombatants(CombatEntity existingCombat, CombatEntity incomingCombat)
    {
        var existingCombatantIds = existingCombat.Combatants.Select(c => c.Id).ToHashSet();
        var incomingCombatantIds = incomingCombat.Combatants.Select(c => c.Id).ToHashSet();

        var combatantsToRemove = existingCombat.Combatants
            .Where(c => !incomingCombatantIds.Contains(c.Id))
            .ToList();

        foreach (var combatant in combatantsToRemove)
        {
            _dbContext.CombatantSkills.RemoveRange(combatant.Skills);
            existingCombat.Combatants.Remove(combatant);
        }

        foreach (var incomingCombatant in incomingCombat.Combatants)
        {
            var existingCombatant = existingCombat.Combatants.FirstOrDefault(c => c.Id == incomingCombatant.Id);

            if (existingCombatant is null)
            {
                existingCombat.Combatants.Add(incomingCombatant);
                continue;
            }

            existingCombatant.SourceKey = incomingCombatant.SourceKey;
            existingCombatant.DisplayName = incomingCombatant.DisplayName;
            existingCombatant.Side = incomingCombatant.Side;
            existingCombatant.Archetype = incomingCombatant.Archetype;
            existingCombatant.MaxVitality = incomingCombatant.MaxVitality;
            existingCombatant.CurrentVitality = incomingCombatant.CurrentVitality;
            existingCombatant.Guard = incomingCombatant.Guard;
            existingCombatant.Mana = incomingCombatant.Mana;
            existingCombatant.Charge = incomingCombatant.Charge;
            existingCombatant.Status = incomingCombatant.Status;

            UpdateCombatantSkills(existingCombatant, incomingCombatant);
        }
    }

    private void UpdateCombatantSkills(CombatantEntity existingCombatant, CombatantEntity incomingCombatant)
    {
        existingCombatant.Skills.Clear();
        existingCombatant.Skills.AddRange(incomingCombatant.Skills);
    }
}
