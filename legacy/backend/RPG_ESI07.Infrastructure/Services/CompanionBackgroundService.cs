using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;

namespace RPG_ESI07.Infrastructure.Services;

public class CompanionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CompanionBackgroundService> _logger;
    private static readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public CompanionBackgroundService(IServiceProvider services, ILogger<CompanionBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            await TickAsync();
        }
    }

    private async Task TickAsync()
    {
        using var scope = _services.CreateScope();
        var companionRepo = scope.ServiceProvider.GetRequiredService<ICompanionRepository>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var state = await companionRepo.GetOrCreateAsync();

            // Récupère les stats de combat du jour
            var today = DateTime.UtcNow.Date;
            var combatStats = await context.CombatStats.ToListAsync();

            var victoriesToday = combatStats.Sum(c => c.CombatsWon);
            var defeatesToday = combatStats.Sum(c => c.CombatsLost);

            // Calcule le prochain état via Markov
            state.CurrentState = CompanionMarkovService.ComputeNextState(state, victoriesToday, defeatesToday);
            state.LastUpdated = DateTime.UtcNow;
            state.VictoriesToday = victoriesToday;
            state.DefeatesToday = defeatesToday;

            await companionRepo.UpdateStateAsync(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du calcul de l'état du companion");
        }
    }
}