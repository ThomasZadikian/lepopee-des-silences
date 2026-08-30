using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.UnitTests.PalaceLaws;

public sealed class PalaceLawTests
{
    [Fact]
    public void Create_ShouldCreatePalaceLaw_WhenInputIsValid()
    {
        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            new[]
            {
                PalaceLawDomain.Narrative,
                PalaceLawDomain.Generation
            });

        law.Id.Value.Should().NotBeEmpty();
        law.Key.Should().Be("law-silence-v1");
        law.Name.Should().Be("Loi du Silence");
        law.Version.Should().Be("1.0.0");
        law.Domains.Should().Contain(PalaceLawDomain.Narrative);
        law.Domains.Should().Contain(PalaceLawDomain.Generation);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenKeyIsEmpty()
    {
        var act = () => PalaceLaw.Create(
            string.Empty,
            "Loi du Silence",
            "1.0.0",
            new[] { PalaceLawDomain.Narrative });

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Palace law key is required.");
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenNoDomainIsProvided()
    {
        var act = () => PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            Array.Empty<PalaceLawDomain>());

        act.Should()
            .Throw<DomainException>()
            .WithMessage("A palace law must target at least one domain.");
    }

    [Fact]
    public void Create_ShouldRemoveDuplicateDomains()
    {
        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            new[]
            {
                PalaceLawDomain.Narrative,
                PalaceLawDomain.Narrative
            });

        law.Domains.Should().ContainSingle();
    }
}