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
            .AsSplitQuery()
            .Include(p => p.Characters)
                .ThenInclude(c => c.StatBlock)
            .Include(p => p.Characters)
                .ThenInclude(c => c.Skills)
            .Include(p => p.Characters)
                .ThenInclude(c => c.Items)
            .Include(p => p.PermanentUnlocks)
            .Include(p => p.PermanentItems)
            .Include(p => p.NpcReputationScores)
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(PlayerProfile profile, CancellationToken cancellationToken)
    {
        var existing = await _context.PlayerProfiles
            .AsSplitQuery()
            .Include(p => p.Characters)
                .ThenInclude(c => c.StatBlock)
            .Include(p => p.Characters)
                .ThenInclude(c => c.Skills)
            .Include(p => p.Characters)
                .ThenInclude(c => c.Items)
            .Include(p => p.PermanentUnlocks)
            .Include(p => p.PermanentItems)
            .Include(p => p.NpcReputationScores)
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
            PalaceShardCount = profile.Progression.PalaceShardCount,
            HimLitShardCount = profile.Progression.HimLitShardCount,
            MainStorySequenceKey = profile.MainStoryProgress.SequenceKey,
            MainStorySequenceVersion = profile.MainStoryProgress.SequenceVersion,
            MainStoryStepKey = profile.MainStoryProgress.StepKey,
            MainStoryCheckpointKey = profile.MainStoryProgress.CheckpointKey,
            MainStoryCompleted = profile.MainStoryProgress.IsCompleted,
            HighestDifficultyLevelUnlocked = profile.MainStoryProgress.HighestDifficultyLevelUnlocked,
            MainStoryUnlockedRoomKeysJson = JsonSerializer.Serialize(profile.MainStoryProgress.UnlockedRoomKeys),
            MainStoryVisibleRoomKeysJson = JsonSerializer.Serialize(profile.MainStoryProgress.VisibleRoomKeys),
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
                StatPointsInvested = c.StatPointsInvested,
                SkillKeysJson = JsonSerializer.Serialize(c.SkillKeys),
                CharacterType = c.CharacterType,
                Status = c.Status,
                ArchetypeKey = c.ArchetypeKey,
                ArchivedAtUtc = c.ArchivedAtUtc,
                CreatedAtUtc = profile.CreatedAtUtc,
                UpdatedAtUtc = profile.UpdatedAtUtc,
                StatBlock = ToStatBlockEntity(c),
                Skills = c.Skills.Select(ToSkillEntity).ToList(),
                Items = c.Items.Select(ToItemEntity).ToList()
            }).ToList(),
            PermanentUnlocks = profile.PermanentUnlocks.Select(u => ToPermanentUnlockEntity(u, profile.Id.Value)).ToList(),
            PermanentItems = profile.PermanentItems.Select(i => ToPermanentItemEntity(i, profile.Id.Value)).ToList(),
            NpcReputationScores = profile.NpcReputationScores.Select(s => ToNpcReputationScoreEntity(s, profile.Id.Value)).ToList()
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
        existing.PalaceShardCount = incoming.Progression.PalaceShardCount;
        existing.HimLitShardCount = incoming.Progression.HimLitShardCount;
        existing.MainStorySequenceKey = incoming.MainStoryProgress.SequenceKey;
        existing.MainStorySequenceVersion = incoming.MainStoryProgress.SequenceVersion;
        existing.MainStoryStepKey = incoming.MainStoryProgress.StepKey;
        existing.MainStoryCheckpointKey = incoming.MainStoryProgress.CheckpointKey;
        existing.MainStoryCompleted = incoming.MainStoryProgress.IsCompleted;
        existing.HighestDifficultyLevelUnlocked = incoming.MainStoryProgress.HighestDifficultyLevelUnlocked;
        existing.MainStoryUnlockedRoomKeysJson = JsonSerializer.Serialize(incoming.MainStoryProgress.UnlockedRoomKeys);
        existing.MainStoryVisibleRoomKeysJson = JsonSerializer.Serialize(incoming.MainStoryProgress.VisibleRoomKeys);
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
                    StatPointsInvested = character.StatPointsInvested,
                    SkillKeysJson = JsonSerializer.Serialize(character.SkillKeys),
                    CharacterType = character.CharacterType,
                    Status = character.Status,
                    ArchetypeKey = character.ArchetypeKey,
                    ArchivedAtUtc = character.ArchivedAtUtc,
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
            existingCharacter.StatPointsInvested = character.StatPointsInvested;
            existingCharacter.SkillKeysJson = JsonSerializer.Serialize(character.SkillKeys);
            existingCharacter.CharacterType = character.CharacterType;
            existingCharacter.Status = character.Status;
            existingCharacter.ArchetypeKey = character.ArchetypeKey;
            existingCharacter.ArchivedAtUtc = character.ArchivedAtUtc;
            existingCharacter.UpdatedAtUtc = incoming.UpdatedAtUtc;

            UpdateStatBlock(existingCharacter, character);
            UpdateSkills(existingCharacter, character);
            UpdateItems(existingCharacter, character);
        }

        UpdatePermanentUnlocks(existing, incoming);
        UpdatePermanentItems(existing, incoming);
        UpdateNpcReputationScores(existing, incoming);
    }

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

    private void UpdatePermanentItems(PlayerProfileEntity existing, PlayerProfile incoming)
    {
        var existingById = existing.PermanentItems.ToDictionary(i => i.Id);

        foreach (var item in incoming.PermanentItems)
        {
            if (existingById.TryGetValue(item.Id.Value, out var existingItem))
            {
                existingItem.ContainedLiquidDefinitionKey = item.ContainedLiquidDefinitionKey;
                continue;
            }

            var newItem = ToPermanentItemEntity(item, incoming.Id.Value);
            existing.PermanentItems.Add(newItem);
            _context.Add(newItem);
        }
    }

    private void UpdateNpcReputationScores(PlayerProfileEntity existing, PlayerProfile incoming)
    {
        var existingByKey = existing.NpcReputationScores
            .ToDictionary(s => s.NpcKey, StringComparer.OrdinalIgnoreCase);

        foreach (var score in incoming.NpcReputationScores)
        {
            if (existingByKey.TryGetValue(score.NpcKey, out var existingScore))
            {
                existingScore.Score = score.Score;
                existingScore.TimesMet = score.TimesMet;
                existingScore.CurrentDialogueNodeKey = score.CurrentDialogueNodeKey;
                existingScore.UpdatedAtUtc = score.UpdatedAtUtc;
                continue;
            }

            var newScore = ToNpcReputationScoreEntity(score, incoming.Id.Value);
            existing.NpcReputationScores.Add(newScore);
            _context.Add(newScore);
        }
    }

    private void UpdateItems(PlayerCharacterEntity existingCharacter, PlayerCharacter character)
    {
        var incomingItemIds = character.Items.Select(i => i.Id.Value).ToHashSet();
        existingCharacter.Items.RemoveAll(i => !incomingItemIds.Contains(i.Id));

        foreach (var item in character.Items)
        {
            var existingItem = existingCharacter.Items.FirstOrDefault(i => i.Id == item.Id.Value);

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
            existingItem.EquipmentPosition = item.Position?.ToString();
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
        statBlock.Focus = character.StatBlock.Focus;
        statBlock.Mana = character.StatBlock.Mana;
        statBlock.Charge = character.StatBlock.Charge;
        statBlock.MagicAttack = character.StatBlock.MagicAttack;
        statBlock.MagicDefense = character.StatBlock.MagicDefense;
        statBlock.Movement = character.StatBlock.Movement;
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
                    c.StatBlock.Focus,
                    c.StatBlock.Mana,
                    c.StatBlock.Charge,
                    c.StatBlock.MagicAttack,
                    c.StatBlock.MagicDefense,
                    c.StatBlock.Movement);

            var skills = c.Skills.Count == 0
                ? (JsonSerializer.Deserialize<List<string>>(c.SkillKeysJson) ?? [])
                    .Select(key => PlayerCharacterSkill.Create(key, c.CreatedAtUtc, "legacy_migration", isEquipped: true))
                    .ToArray()
                : c.Skills
                    .OrderBy(s => s.UnlockedAtUtc)
                    .Select(s => PlayerCharacterSkill.Create(s.SkillDefinitionKey, s.UnlockedAtUtc, s.Source, s.IsEquipped))
                    .ToArray();

            var items = c.Items
                .Select(i => PlayerCharacterItem.Rehydrate(
                    new OwnedItemInstanceId(i.Id),
                    i.ItemDefinitionKey,
                    i.AcquiredAtUtc,
                    i.Source,
                    Enum.TryParse<EquipmentPosition>(i.EquipmentPosition, true, out var position)
                        ? position
                        : null))
                .ToArray();

            return PlayerCharacter.Rehydrate(new PlayerCharacterSnapshot
            {
                Id = new PlayerCharacterId(c.Id),
                DefinitionKey = c.DefinitionKey,
                DisplayName = c.DisplayName,
                CharacterType = c.CharacterType,
                Status = c.Status,
                StatBlock = statBlock,
                Skills = skills,
                Items = items,
                StatPointsInvested = c.StatPointsInvested,
                ArchetypeKey = c.ArchetypeKey,
                ArchivedAtUtc = c.ArchivedAtUtc
            });
        }).ToList();

        var roster = PlayerRoster.Rehydrate(characters);

        var progression = PlayerProgression.Rehydrate(
            entity.TotalRunsStarted,
            entity.TotalRunsCompleted,
            entity.TotalRunsFailed,
            entity.TotalRunsAbandoned,
            entity.UnspentStatPoints,
            entity.TotalStatPointsEarned,
            entity.PalaceShardCount,
            entity.HimLitShardCount);

        var mainStoryProgress = MainStoryProgress.Rehydrate(
            entity.MainStorySequenceKey,
            entity.MainStorySequenceVersion,
            entity.MainStoryStepKey,
            entity.MainStoryCheckpointKey,
            entity.MainStoryCompleted,
            entity.HighestDifficultyLevelUnlocked,
            DeserializeKeys(entity.MainStoryUnlockedRoomKeysJson),
            DeserializeKeys(entity.MainStoryVisibleRoomKeysJson));

        var permanentUnlocks = entity.PermanentUnlocks
            .Select(u => PlayerPermanentUnlock.Create(u.UnlockKey, u.UnlockType, u.SourceRunId, u.UnlockedAtUtc))
            .ToList();

        var permanentItems = entity.PermanentItems
            .Select(i => PlayerPermanentItem.Rehydrate(
                new OwnedItemInstanceId(i.Id), i.ItemDefinitionKey, i.SourceRunId, i.AcquiredAtUtc, i.ContainedLiquidDefinitionKey))
            .ToList();

        var npcReputationScores = entity.NpcReputationScores
            .Select(s => NpcReputationScore.Rehydrate(
                s.NpcKey, s.Score, s.TimesMet, s.CurrentDialogueNodeKey, s.UpdatedAtUtc))
            .ToList();

        return PlayerProfile.Rehydrate(new PlayerProfileSnapshot
        {
            Id = new PlayerId(entity.Id),
            DisplayName = entity.DisplayName,
            Roster = roster,
            Progression = progression,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            MainStoryProgress = mainStoryProgress,
            PermanentUnlocks = permanentUnlocks,
            PermanentItems = permanentItems,
            NpcReputationScores = npcReputationScores
        });
    }

    private static string[] DeserializeKeys(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<string[]>(json) ?? [];

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
            Focus = character.StatBlock.Focus,
            Mana = character.StatBlock.Mana,
            Charge = character.StatBlock.Charge,
            MagicAttack = character.StatBlock.MagicAttack,
            MagicDefense = character.StatBlock.MagicDefense,
            Movement = character.StatBlock.Movement
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
            Id = item.Id.Value,
            ItemDefinitionKey = item.ItemDefinitionKey,
            AcquiredAtUtc = item.AcquiredAtUtc,
            Source = item.Source,
            EquipmentPosition = item.Position?.ToString()
        };
    }

    private static PlayerPermanentItemEntity ToPermanentItemEntity(PlayerPermanentItem item, Guid playerProfileId)
    {
        return new PlayerPermanentItemEntity
        {
            Id = item.Id.Value,
            PlayerProfileId = playerProfileId,
            ItemDefinitionKey = item.ItemDefinitionKey,
            SourceRunId = item.SourceRunId,
            AcquiredAtUtc = item.AcquiredAtUtc,
            ContainedLiquidDefinitionKey = item.ContainedLiquidDefinitionKey
        };
    }

    private static PlayerNpcReputationScoreEntity ToNpcReputationScoreEntity(NpcReputationScore score, Guid playerProfileId)
    {
        return new PlayerNpcReputationScoreEntity
        {
            Id = Guid.NewGuid(),
            PlayerProfileId = playerProfileId,
            NpcKey = score.NpcKey,
            Score = score.Score,
            TimesMet = score.TimesMet,
            CurrentDialogueNodeKey = score.CurrentDialogueNodeKey,
            UpdatedAtUtc = score.UpdatedAtUtc
        };
    }
}
