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
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(PlayerProfile profile, CancellationToken cancellationToken)
    {
        var existing = await _context.PlayerProfiles
            .Include(p => p.Characters)
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
                CreatedAtUtc = profile.CreatedAtUtc
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
                CreatedAtUtc = incoming.CreatedAtUtc
            })
            .ToList();

        existing.Characters.Clear();
        existing.Characters.AddRange(incomingCharacters);
    }

    private static PlayerProfile ToDomain(PlayerProfileEntity entity)
    {
        var characters = entity.Characters.Select(c =>
        {
            var skillKeys = JsonSerializer.Deserialize<List<string>>(c.SkillKeysJson) ?? [];
            return PlayerCharacter.Rehydrate(
                new PlayerCharacterId(c.Id),
                c.DefinitionKey,
                c.DisplayName,
                c.MaxVitality,
                c.BaseMana,
                c.BaseCharge,
                skillKeys);
        }).ToList();

        var roster = PlayerRoster.Rehydrate(characters);

        var progression = PlayerProgression.Rehydrate(
            entity.TotalRunsStarted,
            entity.TotalRunsCompleted,
            entity.TotalRunsFailed,
            entity.TotalRunsAbandoned);

        return PlayerProfile.Rehydrate(
            new PlayerId(entity.Id),
            entity.DisplayName,
            roster,
            progression,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }
}
