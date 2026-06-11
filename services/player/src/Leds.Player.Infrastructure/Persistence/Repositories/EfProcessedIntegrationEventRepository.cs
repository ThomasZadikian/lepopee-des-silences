using Leds.Player.Application.Abstractions;
using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Player.Infrastructure.Persistence.Repositories;

public sealed class EfProcessedIntegrationEventRepository : IProcessedIntegrationEventRepository
{
    private readonly PlayerDbContext _context;

    public EfProcessedIntegrationEventRepository(PlayerDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _context.ProcessedIntegrationEvents
            .AnyAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid eventId, string type, DateTime processedAtUtc, CancellationToken cancellationToken)
    {
        var exists = await _context.ProcessedIntegrationEvents
            .AnyAsync(e => e.EventId == eventId, cancellationToken);

        if (!exists)
        {
            _context.ProcessedIntegrationEvents.Add(new ProcessedIntegrationEventEntity
            {
                EventId = eventId,
                Type = type,
                ProcessedAtUtc = processedAtUtc
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
