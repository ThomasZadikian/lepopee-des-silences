using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RPG_ESI07.Application;
using RPG_ESI07.Application.Configuration;
using RPG_ESI07.Infrastructure;
using RPG_ESI07.Infrastructure.Data;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using RPG_ESI07.Domain.Interfaces;

Log.Logger = new LoggerConfiguration()
.MinimumLevel.Information()
.MinimumLevel.Override(
"Microsoft.AspNetCore",
Serilog.Events.LogEventLevel.Warning)
.Enrich.FromLogContext()
.Enrich.WithProperty("Application", "RPG_ESI07")
.WriteTo.Console(
outputTemplate:
"[{Timestamp:HH:mm:ss} {Level:u3}] "
+ "{Message:lj} "
+ "{Properties:j}{NewLine}{Exception}")
.WriteTo.File(
path: "logs/rpg-.log",
rollingInterval: RollingInterval.Day,
retainedFileCountLimit: 90,
outputTemplate:
"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} "
+ "[{Level:u3}] {Message:lj} "
+ "{Properties:j}{NewLine}{Exception}")
.CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    }); builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application Services (MediatR, FluentValidation, AutoMapper)
builder.Services.AddApplicationServices();

// Infrastructure Services (Repositories)
builder.Services.AddInfrastructureServices();

// JWT Configuration
var jwtSettings = builder.Configuration
.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
    JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme =
    JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
    new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        IssuerSigningKey =
    new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(
    jwtSettings["Secret"]!)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddRateLimiter(options =>
{
    // Policy globale : 60 req/min
    options.GlobalLimiter =
    PartitionedRateLimiter
    .Create<HttpContext, string>(
    context =>
    RateLimitPartition
    .GetFixedWindowLimiter(
    context.Connection
    .RemoteIpAddress?.ToString()
    ?? "unknown",
    _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 60,
        Window = TimeSpan.FromMinutes(1)
    }));
    // Policy login : 5 req / 5 min
    options.AddSlidingWindowLimiter(
    "login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(5);
        opt.SegmentsPerWindow = 5;
    });
    // Policy register : 3 req / heure
    options.AddFixedWindowLimiter(
    "register", opt =>
    {
        opt.PermitLimit = 3;
        opt.Window = TimeSpan.FromHours(1);
    });
    // Response quand limite atteinte
    options.RejectionStatusCode = 429;
});

// Database configuration
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:5173" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext =
    (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("UserId",
    httpContext.User.FindFirst(
    ClaimTypes.NameIdentifier)
    ?.Value ?? "anonymous");
        diagnosticContext.Set("RemoteIP",
    httpContext.Connection
    .RemoteIpAddress?.ToString());
    };
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await context.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(context, hasher);
}

app.Run();