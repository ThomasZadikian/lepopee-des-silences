using Microsoft.Extensions.DependencyInjection;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Repository;
using RPG_ESI07.Infrastructure.Services;

namespace RPG_ESI07.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IEnemyRepository, EnemyRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlayerProfileRepository, CharacterRepository>();
        services.AddScoped<IBestiaryUnlockRepository, BestiaryUnlockRepository>();
        services.AddScoped<ICombatStatsRepository, CombatStatsRepository>();
        services.AddScoped<IGameSaveRepository, GameSaveRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IPlayerInventoryRepository, PlayerInventoryRepository>();
        services.AddScoped<IPlayerSkillRepository, PlayerSkillRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<INpcInteractionRepository, NpcInteractionRepository>();
        services.AddScoped<INpcRepository, NpcRepository>();
        services.AddScoped<ICompanionRepository, CompanionRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();


        // Services
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IMfaService, TotpMfaService>();
        services.AddScoped<IEncryptionService, AesEncryptionService>();
        services.AddHostedService<CompanionBackgroundService>();

        // Caching
        services.AddMemoryCache();

        return services;
    }
}