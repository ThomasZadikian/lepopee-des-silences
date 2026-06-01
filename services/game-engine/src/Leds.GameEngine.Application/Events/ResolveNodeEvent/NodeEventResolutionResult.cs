using Leds.GameEngine.Application.Events.Dtos;

namespace Leds.GameEngine.Application.Events.ResolveNodeEvent;

public sealed record NodeEventResolutionResult(
    NodeEventResolutionKind ResolutionKind,
    string Title,
    string Description,
    bool RequiresPlayerChoice,
    IReadOnlyCollection<NodeEventChoiceDto> Choices,
    IReadOnlyCollection<NarrativeFragmentDto> NarrativeFragments)
{
    public static NodeEventResolutionResult Create(
        NodeEventResolutionKind resolutionKind,
        string title,
        string description,
        bool requiresPlayerChoice = false,
        IReadOnlyCollection<NodeEventChoiceDto>? choices = null,
        IReadOnlyCollection<NarrativeFragmentDto>? narrativeFragments = null)
    {
        return new NodeEventResolutionResult(
            resolutionKind,
            title,
            description,
            requiresPlayerChoice,
            choices ?? Array.Empty<NodeEventChoiceDto>(),
            narrativeFragments ?? Array.Empty<NarrativeFragmentDto>());
    }
}