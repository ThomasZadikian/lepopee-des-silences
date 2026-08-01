using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Leds.GameEngine.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.Infrastructure.Rewards;

public sealed class EfRewardOfferRepository : IRewardOfferRepository
{
    private readonly GameEngineDbContext _dbContext;

    public EfRewardOfferRepository(GameEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RunId runId, RewardOffer rewardOffer, CancellationToken cancellationToken = default)
    {
        var entity = RunPersistenceMapper.ToRewardOfferEntity(rewardOffer, runId.Value);
        _dbContext.RewardOffers.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RewardOffer?> GetByIdAsync(RewardOfferId rewardOfferId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RewardOffers
            .Include(o => o.Options)
            .FirstOrDefaultAsync(o => o.Id == rewardOfferId.Value, cancellationToken);

        return entity is null ? null : RunPersistenceMapper.ToRewardOfferDomain(entity);
    }

    public async Task UpdateAsync(RewardOffer rewardOffer, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RewardOffers
            .Include(o => o.Options)
            .FirstOrDefaultAsync(o => o.Id == rewardOffer.Id.Value, cancellationToken);

        if (entity is null)
            throw new InvalidOperationException($"RewardOffer with id '{rewardOffer.Id.Value}' not found.");

        entity.State = rewardOffer.State.ToString();
        entity.SelectedAtUtc = rewardOffer.State == RewardOfferState.Selected ? DateTime.UtcNow : null;

        // "Loi de la Chandelle" (law.chandelle): a reroll replaces the whole option set
        // with brand-new RewardChoice ids — sync by removing rows no longer present and
        // inserting the current ones, rather than only updating IsSelected on rows that
        // happen to still match by id (which is all SelectChoice ever needed before).
        var currentChoiceIds = rewardOffer.Choices.Select(c => c.Id.Value).ToHashSet();
        foreach (var stale in entity.Options.Where(o => !currentChoiceIds.Contains(o.Id)).ToList())
        {
            entity.Options.Remove(stale);
            _dbContext.Remove(stale);
        }

        var existingIds = entity.Options.Select(o => o.Id).ToHashSet();
        var index = 0;
        foreach (var choice in rewardOffer.Choices)
        {
            if (existingIds.Contains(choice.Id.Value))
            {
                var optionEntity = entity.Options.First(o => o.Id == choice.Id.Value);
                optionEntity.IsSelected = rewardOffer.SelectedChoiceId?.Value == optionEntity.Id;
                optionEntity.SelectionOrder = index;
            }
            else
            {
                entity.Options.Add(new RewardOptionEntity
                {
                    Id = choice.Id.Value,
                    RewardOfferId = entity.Id,
                    RewardType = choice.RewardType.ToString(),
                    Label = choice.Label,
                    Description = choice.Description,
                    PayloadKey = choice.PayloadKey,
                    SourceEnemyKey = choice.SourceEnemyKey,
                    SourceEnemyDisplayName = choice.SourceEnemyDisplayName,
                    PalaceShardCost = choice.PalaceShardCost,
                    HimLitShardCost = choice.HimLitShardCost,
                    IsSelected = rewardOffer.SelectedChoiceId?.Value == choice.Id.Value,
                    SelectionOrder = index
                });
            }

            index++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
