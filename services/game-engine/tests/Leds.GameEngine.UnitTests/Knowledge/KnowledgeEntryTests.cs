using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Knowledge;
using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.UnitTests.Knowledge;

public sealed class KnowledgeEntryTests
{
    [Fact]
    public void Create_ShouldRejectEmptyKey()
    {
        var act = () => KnowledgeEntry.Create(" ", KnowledgeCategory.Fact);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldStartWithNoVersions_AndNotConfirmed()
    {
        var entry = KnowledgeEntry.Create("fact.hall.majordome-real-name", KnowledgeCategory.Person);

        entry.Versions.Should().BeEmpty();
        entry.IsConfirmed.Should().BeFalse();
        entry.ConfirmedVersion.Should().BeNull();
    }

    [Fact]
    public void AddVersion_ShouldAllowContradictoryVersionsToCoexist_WhileUnconfirmed()
    {
        var entry = KnowledgeEntry.Create("fact.hall.majordome-real-name", KnowledgeCategory.Person);

        entry.AddVersion(KnowledgeVersion.Create("Aurel", MemoryProvenance.Rumor));
        entry.AddVersion(KnowledgeVersion.Create("Osric", MemoryProvenance.NpcStated));

        entry.Versions.Should().HaveCount(2);
        entry.IsConfirmed.Should().BeFalse();
    }

    [Fact]
    public void AddVersion_Confirmed_ShouldClearEarlierContradictingVersions()
    {
        var entry = KnowledgeEntry.Create("fact.hall.majordome-real-name", KnowledgeCategory.Person);
        entry.AddVersion(KnowledgeVersion.Create("Aurel", MemoryProvenance.Rumor));
        entry.AddVersion(KnowledgeVersion.Create("Osric", MemoryProvenance.NpcStated));

        entry.AddVersion(KnowledgeVersion.Create("Osric", MemoryProvenance.Confirmed));

        entry.Versions.Should().ContainSingle();
        entry.IsConfirmed.Should().BeTrue();
        entry.ConfirmedVersion!.Value.Should().Be("Osric");
    }

    [Fact]
    public void AddVersion_ShouldThrow_OnceAlreadyConfirmed()
    {
        var entry = KnowledgeEntry.Create("fact.hall.majordome-real-name", KnowledgeCategory.Person);
        entry.AddVersion(KnowledgeVersion.Create("Osric", MemoryProvenance.Confirmed));

        var act = () => entry.AddVersion(KnowledgeVersion.Create("Aurel", MemoryProvenance.Rumor));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rehydrate_ShouldPreserveKeyCategoryAndVersions()
    {
        var entry = KnowledgeEntry.Create("fact.hall.majordome-real-name", KnowledgeCategory.Person);
        entry.AddVersion(KnowledgeVersion.Create("Osric", MemoryProvenance.NpcStated));

        var rehydrated = KnowledgeEntry.Rehydrate(entry.Key, entry.Category, entry.Versions);

        rehydrated.Key.Should().Be(entry.Key);
        rehydrated.Category.Should().Be(entry.Category);
        rehydrated.Versions.Should().BeEquivalentTo(entry.Versions);
    }
}
