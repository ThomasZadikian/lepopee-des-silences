using FluentAssertions;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class ActivePalaceLawAlignmentTests
{
    [Fact]
    public void CreateFromSnapshot_ShouldSetAllFields()
    {
        var law = ActivePalaceLaw.CreateFromSnapshot(
            PalaceLawId.New(),
            "law-test-v1",
            "Test Law",
            "1.0.0",
            [PalaceLawDomain.Combat],
            "Test Law Display",
            "A test law description",
            "UntilRunEnds",
            DateTime.UtcNow,
            expiresAtRoomId: null,
            consumedAtUtc: null);

        law.Key.Should().Be("law-test-v1");
        law.DisplayName.Should().Be("Test Law Display");
        law.Description.Should().Be("A test law description");
        law.Duration.Should().Be("UntilRunEnds");
        law.AppliedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        law.ConsumedAtUtc.Should().BeNull();
        law.IsConsumed.Should().BeFalse();
    }

    [Fact]
    public void Consume_ShouldSetConsumedAtUtc()
    {
        var law = ActivePalaceLaw.CreateFromSnapshot(
            PalaceLawId.New(),
            "law-test-v1",
            "Test Law",
            "1.0.0",
            [PalaceLawDomain.Combat],
            "Test Law Display",
            null,
            "UntilRunEnds",
            DateTime.UtcNow,
            null, null);

        var consumedAt = DateTime.UtcNow;
        law.Consume(consumedAt);

        law.ConsumedAtUtc.Should().Be(consumedAt);
        law.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void Consume_ShouldBeIdempotent()
    {
        var law = ActivePalaceLaw.CreateFromSnapshot(
            PalaceLawId.New(),
            "law-test-v1",
            "Test Law",
            "1.0.0",
            [PalaceLawDomain.Combat],
            "Test Law Display",
            null,
            "UntilRunEnds",
            DateTime.UtcNow,
            null, null);

        var first = DateTime.UtcNow;
        var second = first.AddMinutes(1);
        law.Consume(first);
        law.Consume(second);

        law.ConsumedAtUtc.Should().Be(first);
    }

    [Fact]
    public void Rehydrate_ShouldAcceptAllNewFields()
    {
        var appliedAt = DateTime.UtcNow;
        var consumedAt = DateTime.UtcNow.AddMinutes(5);

        var law = ActivePalaceLaw.Rehydrate(
            PalaceLawId.New(),
            "law-test-v1",
            "Test Law",
            "1.0.0",
            [PalaceLawDomain.Rewards],
            displayName: "Display Name",
            description: "Description",
            duration: "UntilRoomEnds",
            appliedAtUtc: appliedAt,
            expiresAtRoomId: Guid.NewGuid(),
            consumedAtUtc: consumedAt);

        law.DisplayName.Should().Be("Display Name");
        law.Description.Should().Be("Description");
        law.Duration.Should().Be("UntilRoomEnds");
        law.AppliedAtUtc.Should().Be(appliedAt);
        law.ExpiresAtRoomId.Should().NotBeNull();
        law.ConsumedAtUtc.Should().Be(consumedAt);
        law.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void Rehydrate_ShouldUseDefaults_WhenNewFieldsNull()
    {
        var law = ActivePalaceLaw.Rehydrate(
            PalaceLawId.New(),
            "law-test-v1",
            "Test Law",
            "1.0.0",
            [PalaceLawDomain.Combat]);

        law.DisplayName.Should().Be("Test Law");
        law.Description.Should().Be("");
        law.Duration.Should().Be("UntilRunEnds");
        law.IsConsumed.Should().BeFalse();
    }
}
