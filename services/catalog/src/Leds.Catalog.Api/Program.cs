using FluentValidation;
using Leds.Catalog.Application.DependencyInjection;
using Leds.Catalog.Application.Enemies.GetEnemyTemplateByKey;
using Leds.Catalog.Application.Enemies.ListActiveEnemyTemplates;
using Leds.Catalog.Application.Items.GetItemTemplateByKey;
using Leds.Catalog.Application.Items.ListActiveItemTemplates;
using Leds.Catalog.Application.Skills.GetSkillTemplateByKey;
using Leds.Catalog.Application.Skills.ListActiveSkillTemplates;
using Leds.Catalog.Infrastructure.DependencyInjection;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (ValidationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            title = "Validation failed.",
            status = StatusCodes.Status400BadRequest,
            errors = exception.Errors
                .Select(error => error.ErrorMessage)
                .ToArray()
        });
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var catalog = app.MapGroup("/api/v2/catalog")
    .WithTags("Catalog");

catalog.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        service = "catalog",
        status = "Healthy",
        version = "alpha-0.0.4"
    });
})
.WithName("GetCatalogHealth");

catalog.MapGet("/enemies", async (
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var response = await sender.Send(
        new ListActiveEnemyTemplatesQuery(),
        cancellationToken);

    return Results.Ok(response);
})
.WithName("ListActiveEnemyTemplates");

catalog.MapGet("/enemies/{key}", async (
    string key,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var response = await sender.Send(
        new GetEnemyTemplateByKeyQuery(key),
        cancellationToken);

    return response.Template is null
        ? Results.NotFound(new
        {
            title = "Enemy template not found.",
            status = StatusCodes.Status404NotFound
        })
        : Results.Ok(response);
})
.WithName("GetEnemyTemplateByKey");

catalog.MapGet("/skills", async (
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var response = await sender.Send(
        new ListActiveSkillTemplatesQuery(),
        cancellationToken);

    return Results.Ok(response);
})
.WithName("ListActiveSkillTemplates");

catalog.MapGet("/skills/{key}", async (
    string key,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var response = await sender.Send(
        new GetSkillTemplateByKeyQuery(key),
        cancellationToken);

    return response.Template is null
        ? Results.NotFound(new
        {
            title = "Skill template not found.",
            status = StatusCodes.Status404NotFound
        })
        : Results.Ok(response);
})
.WithName("GetSkillTemplateByKey");

catalog.MapGet("/items", async (
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var response = await sender.Send(
        new ListActiveItemTemplatesQuery(),
        cancellationToken);

    return Results.Ok(response);
})
.WithName("ListActiveItemTemplates");

catalog.MapGet("/items/{key}", async (
    string key,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var response = await sender.Send(
        new GetItemTemplateByKeyQuery(key),
        cancellationToken);

    return response.Template is null
        ? Results.NotFound(new
        {
            title = "Item template not found.",
            status = StatusCodes.Status404NotFound
        })
        : Results.Ok(response);
})
.WithName("GetItemTemplateByKey");

app.Run();

public partial class Program
{
}