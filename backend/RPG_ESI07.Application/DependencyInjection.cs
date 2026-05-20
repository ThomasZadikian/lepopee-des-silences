using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RPG_ESI07.Application.Queries.Leaderboard;

namespace RPG_ESI07.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SearchLeaderboardHandler).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        return services;
    }
}