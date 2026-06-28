using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.Application.Events;

/// <summary>
/// Builds the read-model of the NPC's current dialogue node (speaker, lines, eligible
/// choices) for API responses, given the NPC definition and the runtime relationship.
/// </summary>
public static class NpcDialogueViewFactory
{
    public static NpcDialogueViewDto? Build(CatalogNpcDefinition npc, NpcRelationship relationship)
    {
        if (npc.DialogueGraph is null)
        {
            return null;
        }

        var graph = npc.DialogueGraph;
        var nodeKey = relationship.CurrentDialogueNodeKey ?? graph.EntryNodeKey;

        if (!graph.Nodes.TryGetValue(nodeKey, out var node))
        {
            return null;
        }

        var choices = node.Choices
            .Where(c => RequirementsMet(c.Requirements, relationship))
            .Select(c => new NodeEventChoiceDto(c.Key, c.Label, string.Empty))
            .ToArray();

        // The same node reads differently depending on the fracture state (Q6b: felt, not named).
        var lines = relationship.AggregateState switch
        {
            WoundState.Rompu => node.RupturedLines ?? node.Lines,
            WoundState.Tendu => node.TenseLines ?? node.Lines,
            _ => node.Lines
        };

        return new NpcDialogueViewDto(
            npc.Key,
            node.Speaker,
            nodeKey,
            lines.ToArray(),
            choices,
            relationship.AggregateState.ToString(),
            EncounterActive: true);
    }

    private static bool RequirementsMet(
        IReadOnlyCollection<CatalogDialogueRequirement> requirements,
        NpcRelationship relationship)
    {
        foreach (var requirement in requirements)
        {
            switch (requirement.Kind)
            {
                case "FlagPresent":
                    if (requirement.FlagKey is null || !relationship.HasFlag(requirement.FlagKey)) return false;
                    break;

                case "FlagAbsent":
                    if (requirement.FlagKey is not null && relationship.HasFlag(requirement.FlagKey)) return false;
                    break;

                case "WoundStateAtLeast":
                    if (requirement.WoundKey is null ||
                        !Enum.TryParse<WoundState>(requirement.RequiredWoundState, ignoreCase: true, out var required) ||
                        relationship.GetWoundState(requirement.WoundKey) < required)
                    {
                        return false;
                    }
                    break;
            }
        }

        return true;
    }
}