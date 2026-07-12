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
            .AsNoTracking()
            .AsSplitQuery()
            .Include(run => run.Rooms)
                .ThenInclude(room => room.Nodes)
                    .ThenInclude(node => node.ParentNodeLinks)
            .Include(run => run.MemoryFragments)
            .Include(run => run.JournalEntries)
            .Include(run => run.ActivePalaceLaws)
            .Include(run => run.ActiveCurses)
            .Include(run => run.ActiveCombat)
                .ThenInclude(combat => combat!.Combatants)
                    .ThenInclude(combatant => combatant.Skills)
            .Include(run => run.ActiveCombat)
                .ThenInclude(combat => combat!.Combatants)
                    .ThenInclude(combatant => combatant.BaseStatSnapshot)
            .Include(run => run.ActiveCombat)
                .ThenInclude(combat => combat!.Combatants)
                    .ThenInclude(combatant => combatant.RuntimeState)
            .Include(run => run.PlayerState)
                .ThenInclude(ps => ps!.Skills)
            .Include(run => run.RunModifiers)
            .Include(run => run.InventoryItems)
            .Include(run => run.PlayerSnapshot)
                .ThenInclude(snapshot => snapshot!.Characters)
                    .ThenInclude(c => c.StatBlock)
            .Include(run => run.PlayerSnapshot)
                .ThenInclude(snapshot => snapshot!.Characters)
                    .ThenInclude(c => c.Skills)
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

        // Delete existing entity graph first to avoid EF tracking conflicts.
        var existing = await _dbContext.Runs
            .AsSplitQuery()
            .Include(r => r.Rooms)
                .ThenInclude(room => room.Nodes)
                    .ThenInclude(node => node.ParentNodeLinks)
            .Include(r => r.MemoryFragments)
            .Include(r => r.JournalEntries)
            .Include(r => r.ActivePalaceLaws)
            .Include(r => r.ActiveCurses)
            .Include(r => r.ActiveCombat)
                .ThenInclude(combat => combat!.Combatants)
                    .ThenInclude(combatant => combatant.Skills)
            .Include(r => r.ActiveCombat)
                .ThenInclude(combat => combat!.Combatants)
                    .ThenInclude(combatant => combatant.BaseStatSnapshot)
            .Include(r => r.ActiveCombat)
                .ThenInclude(combat => combat!.Combatants)
                    .ThenInclude(combatant => combatant.RuntimeState)
            .Include(r => r.PlayerState)
                .ThenInclude(ps => ps!.Skills)
            .Include(r => r.RunModifiers)
            .Include(r => r.InventoryItems)
            .Include(r => r.PlayerSnapshot)
                .ThenInclude(snapshot => snapshot!.Characters)
                    .ThenInclude(c => c.StatBlock)
            .Include(r => r.PlayerSnapshot)
                .ThenInclude(snapshot => snapshot!.Characters)
                    .ThenInclude(c => c.Skills)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException($"Run with id '{runId}' was not found for update.");
        }

        _dbContext.Runs.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();
        await _dbContext.Combats
            .Where(combat => combat.RunId == runId)
            .ExecuteDeleteAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();

        // Add fresh entity graph.
        var entity = RunPersistenceMapper.ToEntity(run);
        _dbContext.Runs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateActiveCombatStateAsync(Run run, CancellationToken cancellationToken)
    {
        var combat = run.ActiveCombat;
        if (combat is null)
        {
            return;
        }

        var combatId = combat.Id.Value;

        // Load ONLY the small combat subtree (not the run/map). Cost is constant
        // per tick and does NOT grow with run progress (kills the "Room 3" curve).
        var existingCombat = await _dbContext.Combats
            .AsSplitQuery()
            .Include(c => c.Combatants)
                .ThenInclude(cb => cb.RuntimeState)
            .FirstOrDefaultAsync(c => c.Id == combatId, cancellationToken);

        if (existingCombat is null)
        {
            // Combat row already gone (completed/removed on the full path) — the
            // authoritative state was persisted there, nothing hot to save.
            return;
        }

        var incomingCombat = CombatPersistenceMapper.ToEntity(combat, run.Id.Value);

        // Combat scalars: Status, TurnNumber, CurrentTick, ActiveCombatantId, …
        // SetValues copies every mapped scalar by name, so new columns (e.g. ATB)
        // are covered automatically — no field-by-field maintenance.
        _dbContext.Entry(existingCombat).CurrentValues.SetValues(incomingCombat);

        var incomingById = incomingCombat.Combatants.ToDictionary(c => c.Id);

        foreach (var existingCombatant in existingCombat.Combatants)
        {
            if (!incomingById.TryGetValue(existingCombatant.Id, out var incomingCombatant))
            {
                continue; // combatant membership is fixed during a fight
            }

            // Combatant scalars: CurrentVitality, Guard, Mana, Charge, Status,
            // AttackTypeOverride, … (skills & base stats are immutable mid-combat).
            _dbContext.Entry(existingCombatant).CurrentValues.SetValues(incomingCombatant);

            if (existingCombatant.RuntimeState is not null && incomingCombatant.RuntimeState is not null)
            {
                // Runtime ATB state: AtbGaugeValue, ActionRecoveryUntilTick,
                // AtbFillPerTick, CurrentGuard, CurrentVitality, … all via SetValues.
                _dbContext.Entry(existingCombatant.RuntimeState)
                    .CurrentValues.SetValues(incomingCombatant.RuntimeState);
            }
        }

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

    private void UpdateRunModifiers(RunEntity existing, RunEntity incoming)
    {
        var existingIds = existing.RunModifiers.Select(m => m.Id).ToHashSet();
        var incomingIds = incoming.RunModifiers.Select(m => m.Id).ToHashSet();

        // Remove modifiers no longer in the domain (shouldn't normally happen).
        var toRemove = existing.RunModifiers.Where(m => !incomingIds.Contains(m.Id)).ToList();
        foreach (var m in toRemove) existing.RunModifiers.Remove(m);

        foreach (var incomingModifier in incoming.RunModifiers)
        {
            var existingModifier = existing.RunModifiers.FirstOrDefault(m => m.Id == incomingModifier.Id);

            if (existingModifier is null)
            {
                existing.RunModifiers.Add(incomingModifier);
                continue;
            }

            // Only mutable field is ConsumedAtUtc.
            existingModifier.ConsumedAtUtc = incomingModifier.ConsumedAtUtc;
        }
    }

    private void UpdateInventoryItems(RunEntity existing, RunEntity incoming)
    {
        var incomingIds = incoming.InventoryItems.Select(i => i.Id).ToHashSet();

        // Remove items no longer present.
        var toRemove = existing.InventoryItems.Where(i => !incomingIds.Contains(i.Id)).ToList();
        foreach (var item in toRemove) existing.InventoryItems.Remove(item);

        foreach (var incomingItem in incoming.InventoryItems)
        {
            var existingItem = existing.InventoryItems.FirstOrDefault(i => i.Id == incomingItem.Id);

            if (existingItem is null)
            {
                // Create a new entity in the context to avoid tracking conflicts.
                var newItem = new RunItemEntity
                {
                    Id = Guid.NewGuid(),
                    RunId = existing.Id,
                    DefinitionKey = incomingItem.DefinitionKey,
                    DisplayName = incomingItem.DisplayName,
                    Description = incomingItem.Description,
                    Type = incomingItem.Type,
                    Rarity = incomingItem.Rarity,
                    Quantity = incomingItem.Quantity,
                    EffectType = incomingItem.EffectType,
                    EffectAmount = incomingItem.EffectAmount,
                    CreatedAtUtc = incomingItem.CreatedAtUtc
                };
                existing.InventoryItems.Add(newItem);
                continue;
            }

            // Only mutable field is Quantity (stacking consumables).
            existingItem.Quantity = incomingItem.Quantity;
        }
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

    private void UpdatePlayerState(RunEntity existingRun, RunEntity incoming)
    {
        if (incoming.PlayerState is null)
        {
            if (existingRun.PlayerState is not null)
            {
                _dbContext.PlayerRuntimeSkills.RemoveRange(existingRun.PlayerState.Skills);
                _dbContext.PlayerRuntimeStates.Remove(existingRun.PlayerState);
                existingRun.PlayerState = null;
            }

            return;
        }

        if (existingRun.PlayerState is null)
        {
            existingRun.PlayerState = incoming.PlayerState;
            return;
        }

        existingRun.PlayerState.MaxVitality = incoming.PlayerState.MaxVitality;
        existingRun.PlayerState.CurrentVitality = incoming.PlayerState.CurrentVitality;
        existingRun.PlayerState.Guard = incoming.PlayerState.Guard;
        existingRun.PlayerState.Mana = incoming.PlayerState.Mana;
        existingRun.PlayerState.Charge = incoming.PlayerState.Charge;

        existingRun.PlayerState.Skills.Clear();
        existingRun.PlayerState.Skills.AddRange(incoming.PlayerState.Skills);
    }
}
