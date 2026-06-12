using Leds.Catalog.Api.Middleware;
using Leds.Catalog.Application.DependencyInjection;
using Leds.Catalog.Infrastructure.DependencyInjection;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCatalogExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    if (app.Configuration.GetValue<bool>("CatalogSeed:ApplyOnStartup"))
    {
        var persistenceMode = app.Configuration["Persistence:Mode"];
        if (string.Equals(persistenceMode, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            using var scope = app.Services.CreateScope();
            var seedRunner = scope.ServiceProvider.GetRequiredService<CatalogSeedRunner>();
            await seedRunner.ApplyBaseSeedAsync();
        }
    }
}

app.MapControllers();

app.Run();
