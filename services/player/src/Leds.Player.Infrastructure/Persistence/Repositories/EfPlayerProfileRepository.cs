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
                Skills = c.Skills.Select(ToSkillEntity).ToList()
            }).ToList()
        };
    }

    private static void UpdateEntity(PlayerProfileEntity existing, PlayerProfile incoming)
    {
        existing.DisplayName = incoming.DisplayName;
        existing.TotalRunsStarted = incoming.Progression.TotalRunsStarted;
        existing.TotalRunsCompleted = incoming.Progression.TotalRunsCompleted;
        existing.TotalRunsFailed = incoming.Progression.TotalRunsFailed;
        existing.TotalRunsAbandoned = incoming.Progression.TotalRunsAbandoned;
        existing.UnspentStatPoints = incoming.Progression.UnspentStatPoints;
        existing.TotalStatPointsEarned = incoming.Progression.TotalStatPointsEarned;
        existing.UpdatedAtUtc = incoming.UpdatedAtUtc;

        var incomingCharacters = incoming.Roster.Characters
            .Select(c => new PlayerCharacterEntity
            {
                Id = c.Id.Value,
                PlayerProfileId = incoming.Id.Value,
                DefinitionKey = c.DefinitionKey,
                DisplayName = c.DisplayName,
                MaxVitality = c.MaxVitality,
                BaseMana = c.BaseMana,
                BaseCharge = c.BaseCharge,
                SkillKeysJson = JsonSerializer.Serialize(c.SkillKeys),
                CharacterType = c.CharacterType,
                Status = c.Status,
                CreatedAtUtc = incoming.CreatedAtUtc,
                UpdatedAtUtc = incoming.UpdatedAtUtc,
                StatBlock = ToStatBlockEntity(c),
                Skills = c.Skills.Select(ToSkillEntity).ToList()
            })
            .ToList();

        existing.Characters.Clear();
        existing.Characters.AddRange(incomingCharacters);
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

            return PlayerCharacter.Rehydrate(
                new PlayerCharacterId(c.Id),
                c.DefinitionKey,
                c.DisplayName,
                c.CharacterType,
                c.Status,
                statBlock,
                skills);
        }).ToList();

        var roster = PlayerRoster.Rehydrate(characters);

        var progression = PlayerProgression.Rehydrate(
            entity.TotalRunsStarted,
            entity.TotalRunsCompleted,
            entity.TotalRunsFailed,
            entity.TotalRunsAbandoned,
            entity.UnspentStatPoints,
            entity.TotalStatPointsEarned);

        return PlayerProfile.Rehydrate(
            new PlayerId(entity.Id),
            entity.DisplayName,
            roster,
            progression,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
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
}
