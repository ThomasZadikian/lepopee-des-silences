using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events;

/// <summary>
/// Builds the read-model of the NPC's current dialogue node (speaker, lines, eligible
/// choices) for API responses, given the NPC definition and the runtime relationship.
/// </summary>
public static class NpcDialogueViewFactory
{
    public static NpcDialogueViewDto? Build(CatalogNpcDefinition npc, NpcRelationship relationship, Run run)
    {
        if (npc.DialogueGraph is null)
        {
            return new NpcDialogueViewDto(
                npc.Key,
                npc.DisplayName,
                string.Empty,
                new[] { "La figure se tient là, silencieuse." },
                Array.Empty<NodeEventChoiceDto>(),
                relationship.AggregateState.ToString(),
                EncounterActive: false);
        }

        var graph = npc.DialogueGraph;
        var nodeKey = relationship.CurrentDialogueNodeKey ?? graph.EntryNodeKey;

        if (!graph.Nodes.TryGetValue(nodeKey, out var node))
        {
            return new NpcDialogueViewDto(
                npc.Key,
                npc.DisplayName,
                nodeKey,
                new[] { "La figure se tient là, silencieuse." },
                Array.Empty<NodeEventChoiceDto>(),
                relationship.AggregateState.ToString(),
                EncounterActive: false);
        }

        var choices = node.Choices
            .Where(c => RequirementsMet(c.Requirements, relationship, run))
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
        NpcRelationship relationship,
        Run run)
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

                case "RelationshipScoreAtLeast":
                    if (requirement.RequiredRelationshipScore is not int minScore ||
                        relationship.RelationshipScore < minScore)
                    {
                        return false;
                    }
                    break;

                case "PlayerStatsBalanced":
                    if (!IsPlayerStatsBalanced(run)) return false;
                    break;

                case "PlayerStatsUnbalanced":
                    if (IsPlayerStatsBalanced(run)) return false;
                    break;

                case "PlayerHasCompanion":
                    if (requirement.FlagKey is null || !HasCompanion(run, requirement.FlagKey)) return false;
                    break;

                case "PlayerLacksCompanion":
                    if (requirement.FlagKey is not null && HasCompanion(run, requirement.FlagKey)) return false;
                    break;
            }
        }

        return true;
    }

    // Kept in sync with NpcEventChoiceResolver's identically-named/behaved helpers —
    // this one gates which choices are SHOWN, that one gates whether a selected
    // choice's consequences are actually ACCEPTED.
    private static bool IsPlayerStatsBalanced(Run run)
    {
        var stats = new[] { run.Attack, run.Defense, run.Speed, run.Focus };
        var max = stats.Max();
        var min = stats.Min();
        return max <= 0 || (max - min) / (double)max <= 0.5;
    }

    private static bool HasCompanion(Run run, string companionDefinitionKey)
        => run.PlayerSnapshot?.Characters.Any(c =>
            string.Equals(c.DefinitionKey, companionDefinitionKey, StringComparison.OrdinalIgnoreCase)) == true;
}