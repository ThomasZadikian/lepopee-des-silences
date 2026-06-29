namespace Leds.Catalog.Infrastructure.Persistence.Canon;

/// <summary>
/// Marker for confidential, local-only content seeders. Implementations live in
/// git-ignored files and are discovered by assembly scan — so a fresh clone / CI
/// (without the files) still compiles and simply seeds no canon content.
/// </summary>
public interface ICanonContentSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}