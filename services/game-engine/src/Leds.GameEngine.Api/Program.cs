using Leds.GameEngine.Application.DependencyInjection;
using Leds.GameEngine.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGameEngineApplication();
builder.Services.AddGameEngineInfrastructure();

var app = builder.Build();

app.UseMiddleware<Leds.GameEngine.Api.Middleware.ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();