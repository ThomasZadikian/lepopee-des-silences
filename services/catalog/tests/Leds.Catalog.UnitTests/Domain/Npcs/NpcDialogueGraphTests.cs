using FluentAssertions;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Npcs;

public sealed class NpcDialogueGraphTests
{
    [Fact]
    public void Create_ShouldCreateGraph_WithAllProperties()
    {
        var nodes = new Dictionary<string, NpcDialogueNode>
        {
            { "entry", new NpcDialogueNode("entry", "Traveler", new[] { "Bonjour..." }, Array.Empty<NpcDialogueChoice>()) },
            { "response-1", new NpcDialogueNode("response-1", "Traveler", new[] { "Je comprends." }, Array.Empty<NpcDialogueChoice>()) }
        };

        var graph = new NpcDialogueGraph(
            Key: "dialogue-traveler-v1",
            Version: "1.0.0",
            EntryNodeKey: "entry",
            Nodes: nodes);

        graph.Key.Should().Be("dialogue-traveler-v1");
        graph.Version.Should().Be("1.0.0");
        graph.EntryNodeKey.Should().Be("entry");
        graph.Nodes.Should().HaveCount(2);
        graph.Nodes.Should().ContainKey("entry");
        graph.Nodes.Should().ContainKey("response-1");
    }

    [Fact]
    public void Create_ShouldCreateGraph_WithEmptyNodes()
    {
        var graph = new NpcDialogueGraph(
            Key: "dialogue-empty",
            Version: "1.0.0",
            EntryNodeKey: string.Empty,
            Nodes: new Dictionary<string, NpcDialogueNode>());

        graph.Nodes.Should().BeEmpty();
    }

    [Fact]
    public void RecordEquality_ShouldHaveSameKeyAndVersion_WhenCreatedWithSameValues()
    {
        var g1 = new NpcDialogueGraph("key", "v1", "entry", new Dictionary<string, NpcDialogueNode>());
        var g2 = new NpcDialogueGraph("key", "v1", "entry", new Dictionary<string, NpcDialogueNode>());

        g1.Key.Should().Be(g2.Key);
        g1.Version.Should().Be(g2.Version);
        g1.EntryNodeKey.Should().Be(g2.EntryNodeKey);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenKeyDiffers()
    {
        var g1 = new NpcDialogueGraph("key-1", "v1", "entry", new Dictionary<string, NpcDialogueNode>());
        var g2 = new NpcDialogueGraph("key-2", "v1", "entry", new Dictionary<string, NpcDialogueNode>());

        g1.Should().NotBe(g2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenVersionDiffers()
    {
        var g1 = new NpcDialogueGraph("key", "v1", "entry", new Dictionary<string, NpcDialogueNode>());
        var g2 = new NpcDialogueGraph("key", "v2", "entry", new Dictionary<string, NpcDialogueNode>());

        g1.Should().NotBe(g2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenEntryNodeKeyDiffers()
    {
        var g1 = new NpcDialogueGraph("key", "v1", "entry-1", new Dictionary<string, NpcDialogueNode>());
        var g2 = new NpcDialogueGraph("key", "v1", "entry-2", new Dictionary<string, NpcDialogueNode>());

        g1.Should().NotBe(g2);
    }
}
