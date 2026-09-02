using Leds.Player.Application.Abstractions;
using Leds.Player.Infrastructure.Persistence;
using Leds.Player.Infrastructure.Persistence.Repositories;
using Leds.Player.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leds.Player.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlayerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PlayerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PlayerDb")));

        services.AddScoped<IPlayerProfileRepository, EfPlayerProfileRepository>();
        services.AddScoped<IProcessedIntegrationEventRepository, EfProcessedIntegrationEventRepository>();
        services.AddScoped<IAccountStore, EfAccountStore>();
        services.AddScoped<IAccountPrivacyMaintenanceStore, EfAccountPrivacyMaintenanceStore>();
        services.AddScoped<IAccountProfileMaintenance, EfAccountProfileMaintenance>();
        services.AddScoped<IAccountAuditLog, StructuredAccountAuditLog>();
        services.AddSingleton<IAuthenticationSecurity, AuthenticationSecurity>();
        services.AddSingleton<IAccessTokenIssuer, HmacAccessTokenIssuer>();
        services.AddScoped<IAccountEmailSender, SmtpAccountEmailSender>();
        services.AddHttpClient<ICompromisedPasswordChecker, HaveIBeenPwnedPasswordChecker>(client =>
        {
            client.BaseAddress = new UriBuilder(Uri.UriSchemeHttps, "api.pwnedpasswords.com").Uri;
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LEDS/1.0");
        });
        services.AddScoped<PlayerSeedRunner>();

        return services;
    }
}
