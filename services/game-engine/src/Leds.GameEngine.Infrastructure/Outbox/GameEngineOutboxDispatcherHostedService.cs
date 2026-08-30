using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Leds.GameEngine.Infrastructure.Outbox;

public sealed class GameEngineOutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GameEngineOutboxDispatcherHostedService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 20;
    private const int MaxRetryCount = 10;

    public GameEngineOutboxDispatcherHostedService(
        IServiceProvider serviceProvider,
        ILogger<GameEngineOutboxDispatcherHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollingInterval, stoppingToken);

                await DispatchPendingEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling outbox");
            }
        }
    }

    private async Task DispatchPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameEngineDbContext>();
        var httpClient = scope.ServiceProvider.GetRequiredService<IPlayerProjectionClient>();

        var pending = await context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.RetryCount < MaxRetryCount)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        foreach (var message in pending)
        {
            try
            {
                var payload = new RunOutcomeProjectionEnvelope(
                    message.Id,
                    message.Type,
                    message.EventVersion,
                    message.OccurredAtUtc,
                    message.PayloadJson);

                await httpClient.SendRunOutcomeAsync(payload, cancellationToken);

                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.LastError = ex.Message;
                _logger.LogError(ex, "Failed to dispatch outbox event {Id} ({Type})", message.Id, message.Type);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

public record RunOutcomeProjectionEnvelope(
    Guid EventId,
    string Type,
    string EventVersion,
    DateTime OccurredAtUtc,
    string PayloadJson);