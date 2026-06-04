using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.BestiaryUnlocks;

public class CreateBestiaryUnlockHandler : IRequestHandler<CreateBestiaryUnlockCommand, CreateBestiaryUnlockResponse>
{
    private readonly IBestiaryUnlockRepository _repository;

    public CreateBestiaryUnlockHandler(IBestiaryUnlockRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateBestiaryUnlockResponse> Handle(CreateBestiaryUnlockCommand request, CancellationToken cancellationToken)
    {
        // Evite les doublons — même ennemi rencontré plusieurs fois
        var existing = await _repository.GetByPlayerAndEnemyAsync(request.PlayerId, request.EnemyId);
        if (existing != null)
            return new CreateBestiaryUnlockResponse(existing.Id, "Already unlocked.");

        var entity = new BestiaryUnlock
        {
            PlayerId = request.PlayerId,
            EnemyId = request.EnemyId,
            UnlockedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        return new CreateBestiaryUnlockResponse(entity.Id, "BestiaryUnlock created successfully");
    }
}