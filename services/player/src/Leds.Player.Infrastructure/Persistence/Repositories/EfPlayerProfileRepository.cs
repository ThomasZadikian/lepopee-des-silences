using System.Text.Json;
using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Players;
using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Player.Infrastructure.Persistence.Repositories;

public sealed class EfPlayerProfileRepository : IPlayerProfileRepository
{
    private readonly PlayerDbContext _context;

    public EfPlayerProfileRepository(PlayerDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerProfile?> GetByIdAsync(PlayerId id, CancellationToken cancellationToken)
    {
        var entity = await _context.PlayerProfiles
            .Include(p => p.Characters)
                .ThenInclude(c => c.StatBlock)
            .Include(p => p.Characters)
                .ThenInclude(c => c.Skills)
            .Include(p => p.Characters)
                .ThenInclude(c => c.Items)
            .Include(p => p.PermanentUnlocks)
            .Include(p => p.PermanentItems)
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(PlayerProfile profile, CancellationToken cancellationToken)
    {
        var existing = await _context.PlayerProfiles
            .Include(p => p.Characters)
                .ThenInclude(c => c.StatBlock)
            .Include(p => p.Characters)
                .ThenInclude(c => c.Skills)
            .Include(p => p.Characters)
                .ThenInclude(c => c.Items)
            .Include(p => p.PermanentUnlocks)
            .Include(p => p.PermanentItems)
            .FirstOrDefaultAsync(p => p.Id == profile.Id.Value, cancellationToken);

        if (existing is null)
        {
            _context.PlayerProfiles.Add(ToEntity(profile));
        }
        else
        {
            UpdateEntity(existing, profile);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static PlayerProfileEntity ToEntity(PlayerProfile profile)
    {
        return new PlayerProfileEntity
        {
            Id = profile.Id.Value,
            DisplayName = profile.DisplayName,
            TotalRunsStarted = profile.Progression.TotalRunsStarted,
            TotalRunsCompleted = profile.Progression.TotalRunsCompleted,
            TotalRunsFailed = profile.Progression.TotalRunsFailed,
            TotalRunsAbandoned = profile.Progression.TotalRunsAbandoned,
            UnspentStatPoints = profile.Progression.UnspentStatPoints,
            TotalStatPointsEarned = profile.Progression.TotalStatPointsEarned,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc,
            Characters = profile.Roster.Characters.Select(c => new PlayerCharacterEntity
            {
                Id = c.Id.Value,
                PlayerProfileId = profile.Id.Value,
                DefinitionKey = c.DefinitionKey,
                DisplayName = c.DisplayName,
                MaxVitality = c.MaxVitality,
                BaseMana = c.BaseMana,
                BaseCharge = c.BaseCharge,
                SkillKeysJson = JsonSerializer.Serialize(c.SkillKeys),
                CharacterType = c.CharacterType,
                Status = c.Status,
                CreatedAtUtc = profile.CreatedAtUtc,
                UpdatedAtUtc = profile.UpdatedAtUtc,
                StatBlock = ToStatBlockEntity(c),
                Skills = c.Skills.Select(ToSkillEntity).ToList(),
                Items = c.Items.Select(ToItemEntity).ToList()
            }).ToList(),
            PermanentUnlocks = profile.PermanentUnlocks.Select(u => ToPermanentUnlockEntity(u, profile.Id.Value)).ToList(),
            PermanentItems = profile.PermanentItems.Select(i => ToPermanentItemEntity(i, profile.Id.Value)).ToList()
        };
    }

    private void UpdateEntity(PlayerProfileEntity existing, PlayerProfile incoming)
    {
        existing.DisplayName = incoming.DisplayName;
        existing.TotalRunsStarted = incoming.Progression.TotalRunsStarted;
        existing.TotalRunsCompleted = incoming.Progression.TotalRunsCompleted;
        existing.TotalRunsFailed = incoming.Progression.TotalRunsFailed;
        existing.TotalRunsAbandoned = incoming.Progression.TotalRunsAbandoned;
        existing.UnspentStatPoints = incoming.Progression.UnspentStatPoints;
        existing.TotalStatPointsEarned = incoming.Progression.TotalStatPointsEarned;
        existing.UpdatedAtUtc = incoming.UpdatedAtUtc;

        var incomingCharacterIds = incoming.Roster.Characters.Select(c => c.Id.Value).ToHashSet();
        existing.Characters.RemoveAll(c => !incomingCharacterIds.Contains(c.Id));

        foreach (var character in incoming.Roster.Characters)
        {
            var existingCharacter = existing.Characters.FirstOrDefault(c => c.Id == character.Id.Value);
            if (existingCharacter is null)
            {
                var newCharacter = new PlayerCharacterEntity
                {
                    Id = character.Id.Value,
                    PlayerProfileId = incoming.Id.Value,
                    DefinitionKey = character.DefinitionKey,
                    DisplayName = character.DisplayName,
                    MaxVitality = character.MaxVitality,
                    BaseMana = character.BaseMana,
                    BaseCharge = character.BaseCharge,
                    SkillKeysJson = JsonSerializer.Serialize(character.SkillKeys),
                    CharacterType = character.CharacterType,
                    Status = character.Status,
                    CreatedAtUtc = incoming.CreatedAtUtc,
                    UpdatedAtUtc = incoming.UpdatedAtUtc,
                    StatBlock = ToStatBlockEntity(character),
                    Skills = character.Skills.Select(ToSkillEntity).ToList(),
                    Items = character.Items.Select(ToItemEntity).ToList()
                };
                existing.Characters.Add(newCharacter);
                _context.Add(newCharacter);
                continue;
            }

            existingCharacter.DefinitionKey = character.DefinitionKey;
            existingCharacter.DisplayName = character.DisplayName;
            existingCharacter.MaxVitality = character.MaxVitality;
            existingCharacter.BaseMana = character.BaseMana;
            existingCharacter.BaseCharge = character.BaseCharge;
            existingCharacter.SkillKeysJson = JsonSerializer.Serialize(character.SkillKeys);
            existingCharacter.CharacterType = character.CharacterType;
            existingCharacter.Status = character.Status;
            existingCharacter.UpdatedAtUtc = incoming.UpdatedAtUtc;

            UpdateStatBlock(existingCharacter, character);
            UpdateSkills(existingCharacter, character);
            UpdateItems(existingCharacter, character);
        }

        UpdatePermanentUnlocks(existing, incoming);
        UpdatePermanentItems(existing, incoming);
    }

    // Append-only: permanent unlocks are never modified or removed once granted (they're
    // lifetime, not run-scoped) — only append entries not already present by UnlockKey.
    private void UpdatePermanentUnlocks(PlayerProfileEntity existing, PlayerProfile incoming)
    {
        var existingKeys = existing.PermanentUnlocks
            .Select(u => u.UnlockKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var unlock in incoming.PermanentUnlocks)
        {
            if (existingKeys.Contains(unlock.UnlockKey))
                continue;

            var newUnlock = ToPermanentUnlockEntity(unlock, incoming.Id.Value);
            existing.PermanentUnlocks.Add(newUnlock);
            _context.Add(newUnlock);
        }
    }

    // Append-only for the item itself (never removed once owned), same reasoning as
    // UpdatePermanentUnlocks — but ContainedLiquidDefinitionKey is a mutable field on an
    // already-owned container (SFD container/liquid extension) and must be synced in place.
    private void UpdatePermanentItems(PlayerProfileEntity existing, PlayerProfile incoming)
    {
        var existingByKey = existing.PermanentItems
            .ToDictionary(i => i.ItemDefinitionKey, StringComparer.OrdinalIgnoreCase);

        foreach (var item in incoming.PermanentItems)
        {
            if (existingByKey.TryGetValue(item.ItemDefinitionKey, out var existingItem))
            {
                existingItem.ContainedLiquidDefinitionKey = item.ContainedLiquidDefinitionKey;
                continue;
            }

            var newItem = ToPermanentItemEntity(item, incoming.Id.Value);
            existing.PermanentItems.Add(newItem);
            _context.Add(newItem);
        }
    }

    private void UpdateItems(PlayerCharacterEntity existingCharacter, PlayerCharacter character)
    {
        var incomingItemKeys = character.Items
            .Select(i => i.ItemDefinitionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        existingCharacter.Items.RemoveAll(i => !incomingItemKeys.Contains(i.ItemDefinitionKey));

        foreach (var item in character.Items)
        {
            var existingItem = existingCharacter.Items.FirstOrDefault(i =>
                string.Equals(i.ItemDefinitionKey, item.ItemDefinitionKey, StringComparison.OrdinalIgnoreCase));

            if (existingItem is null)
            {
                var newItem = ToItemEntity(item);
                newItem.PlayerCharacterId = existingCharacter.Id;
                existingCharacter.Items.Add(newItem);
                _context.Add(newItem);
                continue;
            }

            existingItem.AcquiredAtUtc = item.AcquiredAtUtc;
            existingItem.Source = item.Source;
            existingItem.IsEquipped = item.IsEquipped;
        }
    }

    private void UpdateSkills(PlayerCharacterEntity existingCharacter, PlayerCharacter character)
    {
        var incomingSkillKeys = character.Skills
            .Select(s => s.SkillDefinitionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        existingCharacter.Skills.RemoveAll(s => !incomingSkillKeys.Contains(s.SkillDefinitionKey));

        foreach (var skill in character.Skills)
        {
            var existingSkill = existingCharacter.Skills.FirstOrDefault(s =>
                string.Equals(s.SkillDefinitionKey, skill.SkillDefinitionKey, StringComparison.OrdinalIgnoreCase));

            if (existingSkill is null)
            {
                var newSkill = ToSkillEntity(skill);
                newSkill.PlayerCharacterId = existingCharacter.Id;
                existingCharacter.Skills.Add(newSkill);
                _context.Add(newSkill);
                continue;
            }

            existingSkill.UnlockedAtUtc = skill.UnlockedAtUtc;
            existingSkill.Source = skill.Source;
            existingSkill.IsEquipped = skill.IsEquipped;
        }
    }

    private void UpdateStatBlock(PlayerCharacterEntity existingCharacter, PlayerCharacter character)
    {
        if (existingCharacter.StatBlock is null)
        {
            var newStatBlock = ToStatBlockEntity(character);
            existingCharacter.StatBlock = newStatBlock;
            _context.Add(newStatBlock);
            return;
        }

        var statBlock = existingCharacter.StatBlock;
        statBlock.MaxVitality = character.StatBlock.MaxVitality;
        statBlock.AttackPower = character.StatBlock.AttackPower;
        statBlock.Defense = character.StatBlock.Defense;
        statBlock.StartingGuard = character.StatBlock.StartingGuard;
        statBlock.Speed = character.StatBlock.Speed;
        statBlock.Initiative = character.StatBlock.Initiative;
        statBlock.Recovery = character.StatBlock.Recovery;
        statBlock.Focus = character.StatBlock.Focus;
        statBlock.Mana = character.StatBlock.Mana;
        statBlock.Charge = character.StatBlock.Charge;
    }

    private static PlayerProfile ToDomain(PlayerProfileEntity entity)
    {
        var characters = entity.Characters.Select(c =>
        {
            var statBlock = c.StatBlock is null
                ? PlayerCharacterStatBlock.Create(
                    c.MaxVitality,
                    attackPower: 12,
                    defense: 6,
                    startingGuard: 0,
                    speed: 10,
                    initiative: 10,
                    recovery: 5,
                    focus: 0,
                    mana: c.BaseMana,
                    charge: c.BaseCharge)
                : PlayerCharacterStatBlock.Create(
                    c.StatBlock.MaxVitality,
                    c.StatBlock.AttackPower,
                    c.StatBlock.Defense,
                    c.StatBlock.StartingGuard,
                    c.StatBlock.Speed,
                    c.StatBlock.Initiative,
                    c.StatBlock.Recovery,
                    c.StatBlock.Focus,
                    c.StatBlock.Mana,
                    c.StatBlock.Charge);

            var skills = c.Skills.Count == 0
                ? (JsonSerializer.Deserialize<List<string>>(c.SkillKeysJson) ?? [])
                    .Select(key => PlayerCharacterSkill.Create(key, c.CreatedAtUtc, "legacy_migration", isEquipped: true))
                    .ToArray()
                : c.Skills
                    .OrderBy(s => s.UnlockedAtUtc)
                    .Select(s => PlayerCharacterSkill.Create(s.SkillDefinitionKey, s.UnlockedAtUtc, s.Source, s.IsEquipped))
                    .ToArray();

            var items = c.Items
                .Select(i => PlayerCharacterItem.Create(i.ItemDefinitionKey, i.AcquiredAtUtc, i.Source, i.IsEquipped))
                .ToArray();

            return PlayerCharacter.Rehydrate(
                new PlayerCharacterId(c.Id),
                c.DefinitionKey,
                c.DisplayName,
                c.CharacterType,
                c.Status,
                statBlock,
                skills,
                items);
        }).ToList();

        var roster = PlayerRoster.Rehydrate(characters);

        var progression = PlayerProgression.Rehydrate(
            entity.TotalRunsStarted,
            entity.TotalRunsCompleted,
            entity.TotalRunsFailed,
            entity.TotalRunsAbandoned,
            entity.UnspentStatPoints,
            entity.TotalStatPointsEarned);

        var permanentUnlocks = entity.PermanentUnlocks
            .Select(u => PlayerPermanentUnlock.Create(u.UnlockKey, u.UnlockType, u.SourceRunId, u.UnlockedAtUtc))
            .ToList();

        var permanentItems = entity.PermanentItems
            .Select(i => PlayerPermanentItem.Rehydrate(
                i.ItemDefinitionKey, i.SourceRunId, i.AcquiredAtUtc, i.ContainedLiquidDefinitionKey))
            .ToList();

        return PlayerProfile.Rehydrate(
            new PlayerId(entity.Id),
            entity.DisplayName,
            roster,
            progression,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            permanentUnlocks,
            permanentItems);
    }

    private static PlayerCharacterStatBlockEntity ToStatBlockEntity(PlayerCharacter character)
    {
        return new PlayerCharacterStatBlockEntity
        {
            Id = Guid.NewGuid(),
            PlayerCharacterId = character.Id.Value,
            MaxVitality = character.StatBlock.MaxVitality,
            AttackPower = character.StatBlock.AttackPower,
            Defense = character.StatBlock.Defense,
            StartingGuard = character.StatBlock.StartingGuard,
            Speed = character.StatBlock.Speed,
            Initiative = character.StatBlock.Initiative,
            Recovery = character.StatBlock.Recovery,
            Focus = character.StatBlock.Focus,
            Mana = character.StatBlock.Mana,
            Charge = character.StatBlock.Charge
        };
    }

    private static PlayerPermanentUnlockEntity ToPermanentUnlockEntity(PlayerPermanentUnlock unlock, Guid playerProfileId)
    {
        return new PlayerPermanentUnlockEntity
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = playerProfileId,
            UnlockKey = unlock.UnlockKey,
            UnlockType = unlock.UnlockType,
            SourceRunId = unlock.SourceRunId,
            UnlockedAtUtc = unlock.UnlockedAtUtc
        };
    }

    private static PlayerCharacterSkillEntity ToSkillEntity(PlayerCharacterSkill skill)
    {
        return new PlayerCharacterSkillEntity
        {
            Id = Guid.NewGuid(),
            SkillDefinitionKey = skill.SkillDefinitionKey,
            UnlockedAtUtc = skill.UnlockedAtUtc,
            Source = skill.Source,
            IsEquipped = skill.IsEquipped
        };
    }

    private static PlayerCharacterItemEntity ToItemEntity(PlayerCharacterItem item)
    {
        return new PlayerCharacterItemEntity
        {
            Id = Guid.NewGuid(),
            ItemDefinitionKey = item.ItemDefinitionKey,
            AcquiredAtUtc = item.AcquiredAtUtc,
            Source = item.Source,
            IsEquipped = item.IsEquipped
        };
    }

    private static PlayerPermanentItemEntity ToPermanentItemEntity(PlayerPermanentItem item, Guid playerProfileId)
    {
        return new PlayerPermanentItemEntity
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = playerProfileId,
            ItemDefinitionKey = item.ItemDefinitionKey,
            SourceRunId = item.SourceRunId,
            AcquiredAtUtc = item.AcquiredAtUtc,
            ContainedLiquidDefinitionKey = item.ContainedLiquidDefinitionKey
        };
    }
}
