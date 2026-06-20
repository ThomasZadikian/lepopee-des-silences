using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence;
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

        foreach (var optionEntity in entity.Options)
        {
            var optionDomain = rewardOffer.Choices
                .FirstOrDefault(c => c.Id.Value == optionEntity.Id);

            if (optionDomain is not null)
            {
                optionEntity.IsSelected = rewardOffer.SelectedChoiceId?.Value == optionEntity.Id;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
