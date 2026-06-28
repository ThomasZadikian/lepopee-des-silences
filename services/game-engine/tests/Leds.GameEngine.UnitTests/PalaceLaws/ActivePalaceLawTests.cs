using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.UnitTests.PalaceLaws;

public sealed class ActivePalaceLawTests
{
    [Fact]
    public void From_ShouldCreateActiveLaw_FromPalaceLaw()
    {
        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            new[] { PalaceLawDomain.Narrative, PalaceLawDomain.Generation });

        var active = ActivePalaceLaw.From(law);

        active.LawId.Should().Be(law.Id);
        active.Key.Should().Be("law-silence-v1");
        active.Name.Should().Be("Loi du Silence");
        active.Version.Should().Be("1.0.0");
        active.Domains.Should().Contain(PalaceLawDomain.Narrative);
        active.Domains.Should().Contain(PalaceLawDomain.Generation);
        active.DisplayName.Should().Be("Loi du Silence");
        active.Description.Should().BeEmpty();
        active.Duration.Should().Be("UntilRunEnds");
        active.AppliedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        active.ExpiresAtRoomId.Should().BeNull();
        active.ConsumedAtUtc.Should().BeNull();
        active.IsConsumed.Should().BeFalse();
    }



    [Fact]
    public void From_ShouldThrow_WhenLawIsNull()
    {
        var act = () => ActivePalaceLaw.From(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Consume_ShouldSetConsumedAtUtc()
    {
        var law = PalaceLaw.Create("law-v1", "Law", "1.0.0", new[] { PalaceLawDomain.Narrative });
        var active = ActivePalaceLaw.From(law);
        var consumedAt = DateTime.UtcNow;

        active.Consume(consumedAt);

        active.ConsumedAtUtc.Should().Be(consumedAt);
        active.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void Consume_ShouldNotChangeConsumedAtUtc_WhenAlreadyConsumed()
    {
        var law = PalaceLaw.Create("law-v1", "Law", "1.0.0", new[] { PalaceLawDomain.Narrative });
        var active = ActivePalaceLaw.From(law);
        var firstConsumedAt = DateTime.UtcNow;
        var secondConsumedAt = DateTime.UtcNow.AddMinutes(5);

        active.Consume(firstConsumedAt);
        active.Consume(secondConsumedAt);

        active.ConsumedAtUtc.Should().Be(firstConsumedAt);
    }

    [Fact]
    public void CreateFromSnapshot_ShouldRestoreActiveLaw_WithGivenValues()
    {
        var lawId = PalaceLawId.New();
        var appliedAt = DateTime.UtcNow.AddDays(-1);
        var consumedAt = DateTime.UtcNow;
        var expiresAtRoomId = Guid.NewGuid();

        var active = ActivePalaceLaw.CreateFromSnapshot(
            lawId,
            "law-v1",
            "Law",
            "1.0.0",
            new[] { PalaceLawDomain.Narrative },
            "Display Law",
            "Description",
            "UntilRoomEnds",
            appliedAt,
            expiresAtRoomId,
            consumedAt);

        active.LawId.Should().Be(lawId);
        active.Key.Should().Be("law-v1");
        active.Name.Should().Be("Law");
        active.Version.Should().Be("1.0.0");
        active.DisplayName.Should().Be("Display Law");
        active.Description.Should().Be("Description");
        active.Duration.Should().Be("UntilRoomEnds");
        active.AppliedAtUtc.Should().Be(appliedAt);
        active.ExpiresAtRoomId.Should().Be(expiresAtRoomId);
        active.ConsumedAtUtc.Should().Be(consumedAt);
        active.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreActiveLaw_WithGivenValues()
    {
        var lawId = PalaceLawId.New();
        var appliedAt = DateTime.UtcNow;

        var active = ActivePalaceLaw.Rehydrate(
            lawId,
            "law-v1",
            "Law",
            "1.0.0",
            new[] { PalaceLawDomain.Generation },
            appliedAtUtc: appliedAt);

        active.LawId.Should().Be(lawId);
        active.Key.Should().Be("law-v1");
        active.Domains.Should().ContainSingle().Which.Should().Be(PalaceLawDomain.Generation);
        active.DisplayName.Should().Be("Law");
        active.Duration.Should().Be("UntilRunEnds");
        active.AppliedAtUtc.Should().Be(appliedAt);
        active.IsConsumed.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_ShouldAcceptConsumedAtUtc()
    {
        var lawId = PalaceLawId.New();
        var consumedAt = DateTime.UtcNow;

        var active = ActivePalaceLaw.Rehydrate(
            lawId,
            "law-v1",
            "Law",
            "1.0.0",
            new[] { PalaceLawDomain.Narrative },
            consumedAtUtc: consumedAt);

        active.ConsumedAtUtc.Should().Be(consumedAt);
        active.IsConsumed.Should().BeTrue();
    }
}
