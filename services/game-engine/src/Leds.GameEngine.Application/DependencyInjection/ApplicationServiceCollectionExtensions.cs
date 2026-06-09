using FluentValidation;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Common.Behaviors;
using Leds.GameEngine.Application.Events.ChoiceResolvers;
using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Application.Events.Resolvers;
using Leds.GameEngine.Application.Interlude;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Runs.StartRun;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.GameEngine.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddGameEngineApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(StartRunCommand).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<INodeEventResolverDispatcher, NodeEventResolverDispatcher>();

        services.AddScoped<INodeEventResolver, CombatNodeEventResolver>();
        services.AddScoped<INodeEventResolver, EliteNodeEventResolver>();
        services.AddScoped<INodeEventResolver, ItemNodeEventResolver>();
        services.AddScoped<INodeEventResolver, NpcNodeEventResolver>();
        services.AddScoped<INodeEventResolver, RestNodeEventResolver>();
        services.AddScoped<INodeEventResolver, MerchantNodeEventResolver>();
        services.AddScoped<INodeEventResolver, LawNodeEventResolver>();
        services.AddScoped<INodeEventResolver, CurseNodeEventResolver>();
        services.AddScoped<INodeEventResolver, RareNodeEventResolver>();
        services.AddScoped<INodeEventResolver, RoomBossNodeEventResolver>();
        services.AddScoped<INodeEventResolver, FinalBossNodeEventResolver>();
        services.AddScoped<ICurrentEventChoiceResolverDispatcher, CurrentEventChoiceResolverDispatcher>();
        services.AddScoped<ICurrentEventChoiceResolver, NpcEventChoiceResolver>();
        services.AddScoped<ICurrentEventChoiceResolver, LawEventChoiceResolver>();
        services.AddScoped<ICurrentEventChoiceResolver, MerchantEventChoiceResolver>();
        services.AddScoped<ICurrentEventChoiceResolver, CurseEventChoiceResolver>();
        services.AddScoped<ICurrentEventChoiceRequirementResolver, CurrentEventChoiceRequirementResolver>();
        services.AddScoped<IPalaceLawCatalog, StaticPalaceLawCatalog>();
        services.AddScoped<ICombatRiskProfileResolver, CombatRiskProfileResolver>();
        services.AddScoped<RewardOfferFactory>();
        services.AddScoped<IInterludeNodeProvider, DefaultInterludeNodeProvider>();
        services.AddSingleton<ICombatFactory, CombatFactory>();

        return services;
    }
}