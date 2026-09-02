using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Leds.Player.Api.Middleware;
using Leds.Player.Application.DependencyInjection;
using Leds.Player.Infrastructure.DependencyInjection;
using Leds.Player.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPlayerApplication();
builder.Services.AddPlayerInfrastructure(builder.Configuration);

var signingKey = builder.Configuration["Authentication:Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
    {
        signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        builder.Configuration["Authentication:Jwt:SigningKey"] = signingKey;
    }
    else
    {
        throw new InvalidOperationException("Authentication:Jwt:SigningKey must be configured.");
    }
}

if (string.IsNullOrWhiteSpace(builder.Configuration["Authentication:MfaProtectionKey"])
    && (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")))
{
    builder.Configuration["Authentication:MfaProtectionKey"] = Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(32));
}

if (builder.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Email:Mode"]))
{
    builder.Configuration["Authentication:Email:Mode"] = "Log";
}

var issuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? "leds-player";
var audience = builder.Configuration["Authentication:Jwt:Audience"] ?? "leds-game-client";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<PlayerDbContext>();
    await db.Database.MigrateAsync();

    var seedRunner = scope.ServiceProvider.GetRequiredService<PlayerSeedRunner>();
    await seedRunner.ApplyDemoPlayerSeedAsync();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
