using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RPG_ESI07.Infrastructure.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RPG_ESI07.Tests.Integration;

public class AuthRateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthRateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();

                config.AddJsonFile("appsettings.json", optional: true);

                var testDirectory = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location)!;
                config.AddJsonFile(
                    Path.Combine(testDirectory, "appsettings.Test.json"),
                    optional: false);

                config.AddEnvironmentVariables();
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<DbContextOptions>();

                var efInternalServices = services
                    .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                    .ToList();

                foreach (var service in efInternalServices)
                {
                    services.Remove(service);
                }

                var internalServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("RateLimitTestsDb")
                           .UseInternalServiceProvider(internalServiceProvider);
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Login_Policy_Triggers_After_Five_Requests()
    {
        for (int i = 0; i < 5; i++)
        {
            var content = new StringContent(
                "{\"username\":\"test\",\"password\":\"pass\"}",
                Encoding.UTF8,
                "application/json");
            await _client.PostAsync("/api/auth/login", content);
        }

        var lastContent = new StringContent(
            "{\"username\":\"test\",\"password\":\"pass\"}",
            Encoding.UTF8,
            "application/json");
        var response = await _client.PostAsync("/api/auth/login", lastContent);

        response.StatusCode.Should().Be((HttpStatusCode)429);
    }
}