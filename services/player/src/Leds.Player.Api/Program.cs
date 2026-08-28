using Leds.Player.Api.Middleware;
using Leds.Player.Application.DependencyInjection;
using Leds.Player.Infrastructure.DependencyInjection;
using Leds.Player.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddPlayerApplication();
builder.Services.AddPlayerInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PlayerDbContext>();
    await dbContext.Database.MigrateAsync();

    var seedRunner = scope.ServiceProvider.GetRequiredService<PlayerSeedRunner>();
    await seedRunner.ApplyDemoPlayerSeedAsync();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health/live");

app.Run();

public partial class Program;
