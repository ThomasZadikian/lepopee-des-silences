using Leds.Catalog.Api.Middleware;
using Leds.Catalog.Application.DependencyInjection;
using Leds.Catalog.Infrastructure.DependencyInjection;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCatalogExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    if (app.Environment.IsDevelopment())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    if (app.Configuration.GetValue<bool>("CatalogSeed:ApplyOnStartup"))
    {
        var seedRunner = scope.ServiceProvider.GetRequiredService<CatalogSeedRunner>();
        await seedRunner.ApplyBaseSeedAsync();

        var externalSeeder = scope.ServiceProvider.GetRequiredService<ExternalCatalogContentSeeder>();
        await externalSeeder.ApplyAsync();
    }

    var integrityValidator = scope.ServiceProvider.GetRequiredService<CatalogIntegrityValidator>();
    await integrityValidator.ValidateAsync();
}

app.MapControllers();
app.MapHealthChecks("/health/live");

app.Run();

public partial class Program;
