using Leds.GameEngine.Api.Middleware;
using Leds.GameEngine.Api.DevTools;
using Leds.GameEngine.Application.DependencyInjection;
using Leds.GameEngine.Infrastructure.DependencyInjection;
using Leds.GameEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

const string CorsPolicyName = "LedsCorsPolicy";
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGameEngineApplication();
builder.Services.AddGameEngineInfrastructure(builder.Configuration);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Correlation-Id");
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<GameEngineDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapGameEngineDevTools();

app.Run();

public partial class Program;
