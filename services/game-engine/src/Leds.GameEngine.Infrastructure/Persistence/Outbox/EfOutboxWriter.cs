using Leds.GameEngine.Infrastructure.Persistence.Entities;
using System.Text.Json;

namespace Leds.GameEngine.Infrastructure.Persistence.Outbox;

public sealed class EfOutboxWriter : IEfOutboxWriter
{
    private readonly GameEngineDbContext _context;

    public EfOutboxWriter(GameEngineDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync<TEvent>(
        TEvent integrationEvent,
        string type,
        string eventVersion,
        string destination,
        DateTime occurredAtUtc,
        Guid? correlationId,
        Guid? causationId,
        CancellationToken cancellationToken)
    {
        var entity = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            Type = type,
            EventVersion = eventVersion,
            PayloadJson = JsonSerializer.Serialize(integrationEvent),
            OccurredAtUtc = occurredAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            Destination = destination,
            CorrelationId = correlationId,
            CausationId = causationId
        };

        _context.OutboxMessages.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public interface IEfOutboxWriter
{
    Task AddAsync<TEvent>(
        TEvent integrationEvent,
        string type,
        string eventVersion,
        string destination,
        DateTime occurredAtUtc,
        Guid? correlationId,
        Guid? causationId,
        CancellationToken cancellationToken);
}