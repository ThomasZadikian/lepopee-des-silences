using Leds.Catalog.Api.Middleware;
using Leds.Catalog.Application.DependencyInjection;
using Leds.Catalog.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCatalogExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();