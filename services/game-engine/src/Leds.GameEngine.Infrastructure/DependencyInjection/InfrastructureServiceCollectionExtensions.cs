using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.Actions;
using Leds.GameEngine.Application.Combats.Effects;
using Leds.GameEngine.Application.Combats.EncounterComposition;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Application.Combats.EnemyTurns;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Combats.Targeting;
using Leds.GameEngine.Application.Combats.Typing;
using Leds.GameEngine.Application.Effects;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Events.Resolution;
using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Application.PalaceLaws.Ports;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.RoomMaps;
using Leds.GameEngine.Application.Selection.Ports;
using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Selection;
using Leds.GameEngine.Infrastructure.Catalog;
using Leds.GameEngine.Infrastructure.Combats;
using Leds.GameEngine.Infrastructure.Combats.Actions;
using Leds.GameEngine.Infrastructure.Combats.EncounterComposition;
using Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;
using Leds.GameEngine.Infrastructure.Combats.Targeting;
using Leds.GameEngine.Infrastructure.Events.Resolution;
using Leds.GameEngine.Infrastructure.Generation;
using Leds.GameEngine.Infrastructure.Generation.Psyche;
using Leds.GameEngine.Infrastructure.Generation.Randomness;
using Leds.GameEngine.Infrastructure.Generation.RoomMaps;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Reachability;
using Leds.GameEngine.Infrastructure.Generation.Rooms.States;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;
using Leds.GameEngine.Infrastructure.Markov;
using Leds.GameEngine.Infrastructure.Outbox;
using Leds.GameEngine.Infrastructure.PalaceLaws;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Repositories;
using Leds.GameEngine.Infrastructure.Players;
using Leds.GameEngine.Infrastructure.Rewards;
using Leds.GameEngine.Infrastructure.Selection;
using Leds.SharedBuildingBlocks.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leds.GameEngine.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddGameEngineInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();

        services.AddDbContext<GameEngineDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("GameEngineDb")));

        services.AddScoped<IRunRepository, EfRunRepository>();
        services.AddScoped<ICombatActionRecordRepository, EfCombatActionRecordRepository>();
        services.AddScoped<IRewardOfferRepository, EfRewardOfferRepository>();
        services.AddScoped<ISelectionDecisionRepository, EfSelectionDecisionRepository>();
        services.AddScoped<IPalaceIndicatorRepository, EfPalaceIndicatorRepository>();

        services.AddSingleton<ISeededRandomFactory, SeededRandomFactory>();

        services.AddSingleton<EmotionalCalibration>();
        services.AddSingleton<IRunPsycheEvolver, RunPsycheEvolver>();
        services.AddSingleton<IMarkovTransitionTraceSink, NullMarkovTransitionTraceSink>();
        services.AddSingleton<DeterministicMarkovSampler>();
        services.AddSingleton<MarkovTransitionResolver>();
        services.AddSingleton<IRoomTypeMarkovMatrixProvider, StaticRoomTypeMarkovMatrixProvider>();
        services.AddSingleton<IRoomTypeResolver, MarkovRoomTypeResolver>();
        services.AddSingleton<ICatalogRoomTypeResolver, CatalogMarkovRoomTypeResolver>();
        services.AddSingleton<IRoomReachabilitySelector, RoomReachabilitySelector>();
        services.AddSingleton<IPalaceRoomStateResolver, MarkovPalaceRoomStateResolver>();
        services.AddSingleton<IRoomThemeResolver, RoomThemeResolver>();
        services.AddSingleton<IRoomBossProfileResolver, RoomBossProfileResolver>();

        services.AddSingleton<IRunGenerator, DeterministicRunGenerator>();

        services.AddSingleton<IRoomTypeGenerationProfileProvider, HardcodedRoomTypeGenerationProfileProvider>();
        services.AddSingleton<IGridRoomLayoutTemplateProvider, GridRoomLayoutTemplateProvider>();
        services.AddSingleton<IGridRoomGenerator, GridRoomGenerator>();

        services.Configure<CatalogGatewayOptions>(
            configuration.GetSection(CatalogGatewayOptions.SectionName));

        services.AddHttpClient<ICatalogContentGateway, HttpCatalogContentGateway>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<CatalogGatewayOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            });

        services.Configure<PlayerGatewayOptions>(
            configuration.GetSection(PlayerGatewayOptions.SectionName));

        services.AddHttpClient<IPlayerRunSnapshotGateway, HttpPlayerRunSnapshotGateway>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<PlayerGatewayOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            });

        services.AddHttpClient<IPlayerProfileGateway, HttpPlayerProfileGateway>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<PlayerGatewayOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            });

        RegisterOutbox(services, configuration);

        services.AddSingleton<IEventContentResolver, EventContentResolver>();

        services.AddSingleton<IEventContentResolutionStrategy, CombatEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, RoomBossEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, ItemEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, PalaceLawEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, NpcEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, RestEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, MerchantEventContentResolutionStrategy>();
        services.AddSingleton<IEventContentResolutionStrategy, RareEventContentResolutionStrategy>();

        services.AddSingleton<ICombatInstanceFactory, CombatInstanceFactory>();

        services.AddSingleton<ICombatTargetingRuleValidator, CombatTargetingRuleValidator>();
        services.AddSingleton<ICombatSkillActionValidator, CombatSkillActionValidator>();
        services.AddSingleton<ICombatantTypeProfileProvider, EmotionalTypeProfileProvider>();
        services.AddSingleton<ICombatSkillEffectResolver, CombatSkillEffectResolver>();
        services.AddSingleton<Leds.GameEngine.Application.Combats.EnemyTurns.Ai.IEnemyActionPlanner,
            Leds.GameEngine.Application.Combats.EnemyTurns.Ai.UtilityEnemyActionPlanner>();
        services.AddSingleton<IEnemyCombatTurnResolver, EnemyCombatTurnResolver>();
        RegisterCanonBossBehaviors(services);
        services.AddSingleton<IEncounterCompositionPolicy, EncounterCompositionPolicy>();
        services.AddSingleton<IEncounterEnemySelector, DeterministicEncounterEnemySelector>();
        services.AddSingleton<ICombatEncounterDraftGenerator, CombatEncounterDraftGenerator>();

        services.AddSingleton<DeterministicWeightedSelector>();

        services.AddSingleton<IRuntimeEffectResolver, RuntimeEffectResolver>();

        return services;
    }

    private static void RegisterOutbox(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<Persistence.Outbox.IEfOutboxWriter, Persistence.Outbox.EfOutboxWriter>();
        services.AddScoped<Application.Abstractions.IOutboxWriter, Persistence.Outbox.EfOutboxWriter>();

        services.AddHttpClient<IPlayerProjectionClient, HttpPlayerProjectionClient>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<PlayerGatewayOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        var outboxEnabled = configuration.GetValue<bool>("Outbox:DispatcherEnabled", true);

        if (outboxEnabled)
        {
            services.AddHostedService<GameEngineOutboxDispatcherHostedService>();
        }
    }

    // Scripted phases for the canon room bosses. Each is matched against the boss
    // combatant's SourceKey; an enemy with no registered behavior uses the generic AI.
    private static void RegisterCanonBossBehaviors(IServiceCollection services)
    {
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.ImperatriceVipereBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.PapeLouisXviiBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.HomonculeRoiBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.GrandCardinalBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.HimLitBossBehavior>();

        // Bestiaire — Les Veilleurs du Seuil (famille 1).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.VeilleurTapisBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.PorteurPlateauBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.EchoPolitesseBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.SentinelleSeuilBossBehavior>();

        // Bestiaire — Les Copistes (famille 2).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.CopisteAveugleBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.EncrierVivantBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.PageInacheveeBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.RelieurBossBehavior>();

        // Bestiaire — Les Squelettes de Souvenirs (famille 3).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.SqueletteSouvenirBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.PorteurCendreBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.ChoeurMuetBossBehavior>();

        // Bestiaire — Les Chimères des Plaines (famille 4).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.ChimereAffameeBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.BergerOrdresBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.AgneauInverseBossBehavior>();

        // Bestiaire — Les Créations du Forgeron (famille 5).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.CreationInstableBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.MarteauVivantBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.SentinelleFonteBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.ScorieRampanteBossBehavior>();

        // Bestiaire — Les Blouses Blanches (famille 6).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.InfirmiereDeniBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.SouvenirAliteBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.RegisseurBlancBossBehavior>();

        // Bestiaire — Les Pénitents de la Montagne (famille 7).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.PelerinSansVisageBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.PrieurLituiqueBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.FrayeurExhumeeBossBehavior>();

        // Bestiaire — Les Faux Habitants du Jardin (famille 8).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.PromeneurFigeBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.JardinierSansOmbreBossBehavior>();

        // Bestiaire — Les Gardiens de Crystal (famille 9).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.GardienIntemporelBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.EclatEveilleBossBehavior>();

        // Bestiaire — Les Échos d'Émotions (famille 10).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.EchoColereBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.EchoPeurBossBehavior>();
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.EchoTristesseBossBehavior>();

        // Bestiaire — L'Impératrice de la Falaise (mini-boss).
        services.AddSingleton<IBossBehavior, Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon.ImperatriceBossBehavior>();
    }
}
