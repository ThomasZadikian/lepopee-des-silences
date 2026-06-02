namespace Leds.Catalog.Api.Middleware;

public static class CatalogExceptionHandlingExtensions
{
    public static IApplicationBuilder UseCatalogExceptionHandling(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<CatalogExceptionHandlingMiddleware>();
    }
}