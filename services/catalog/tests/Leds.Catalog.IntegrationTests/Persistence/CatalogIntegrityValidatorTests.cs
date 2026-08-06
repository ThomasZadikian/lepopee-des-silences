using FluentAssertions;
using Leds.Catalog.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace Leds.Catalog.IntegrationTests.Persistence;

[Collection("CatalogPostgres")]
public sealed class CatalogIntegrityValidatorTests
{
    private readonly CatalogPostgresFixture _fixture;

    public CatalogIntegrityValidatorTests(CatalogPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Canonical_seed_should_pass_the_cross_definition_publication_gate()
    {
        await using var context = _fixture.CreateContext().Context;
        var seed = new CatalogSeedRunner(context, NullLogger<CatalogSeedRunner>.Instance);
        await seed.ApplyBaseSeedAsync();
        var validator = new CatalogIntegrityValidator(context);

        var act = () => validator.ValidateAsync();

        await act.Should().NotThrowAsync();
    }
}
