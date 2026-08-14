using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Knowledge;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Knowledge;

public sealed class RunKnowledgeTests
{
    [Fact]
    public void GetOrCreateKnowledgeEntry_ShouldCreateOnFirstReference()
    {
        var run = TestGameEngineFactory.CreateRun();

        var entry = run.GetOrCreateKnowledgeEntry("fact.hall.majordome-real-name", KnowledgeCategory.Person);

        entry.Key.Should().Be("fact.hall.majordome-real-name");
        run.KnowledgeEntries.Should().ContainSingle();
    }

    [Fact]
    public void GetOrCreateKnowledgeEntry_ShouldReturnTheSameInstance_OnASecondReference()
    {
        var run = TestGameEngineFactory.CreateRun();
        var first = run.GetOrCreateKnowledgeEntry("fact.hall.majordome-real-name", KnowledgeCategory.Person);

        var second = run.GetOrCreateKnowledgeEntry("fact.hall.majordome-real-name", KnowledgeCategory.Person);

        second.Should().BeSameAs(first);
        run.KnowledgeEntries.Should().ContainSingle();
    }

    [Fact]
    public void GetOrCreateKnowledgeEntry_ShouldThrow_OnACategoryMismatch()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.GetOrCreateKnowledgeEntry("fact.hall.majordome-real-name", KnowledgeCategory.Person);

        var act = () => run.GetOrCreateKnowledgeEntry("fact.hall.majordome-real-name", KnowledgeCategory.Fact);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GetKnowledgeEntry_ShouldReturnNull_WhenNeverReferenced()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.GetKnowledgeEntry("fact.unknown").Should().BeNull();
    }

    [Fact]
    public void RehydrateKnowledgeEntry_ShouldRestoreIt()
    {
        var run = TestGameEngineFactory.CreateRun();
        var entry = KnowledgeEntry.Create("fact.hall.majordome-real-name", KnowledgeCategory.Person);
        entry.AddVersion(KnowledgeVersion.Create("Osric", MemoryProvenance.Confirmed));

        run.RehydrateKnowledgeEntry(entry);

        run.GetKnowledgeEntry("fact.hall.majordome-real-name")!.ConfirmedVersion!.Value.Should().Be("Osric");
    }
}
