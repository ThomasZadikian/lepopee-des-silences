namespace Leds.GameEngine.Domain.Interlude;

/// <summary>
/// A node in the interlude hub displayed between two rooms.
///
/// InterludeNodes are intentionally distinct from MapNodes:
/// they carry no riskLevel, rewardProfile, parentNodeIds, row/lane,
/// or MapNode state — they are purely navigational/consultative.
/// </summary>
public sealed class InterludeNode
{
    private InterludeNode(
        Guid id,
        InterludeNodeType type,
        string label,
        string description,
        string positionSlot,
        bool isEnabled,
        InterludeActionKey actionKey)
    {
        Id = id;
        Type = type;
        Label = label;
        Description = description;
        PositionSlot = positionSlot;
        IsEnabled = isEnabled;
        ActionKey = actionKey;
    }

    /// <summary>Stable identifier for this node within the interlude session.</summary>
    public Guid Id { get; }

    /// <summary>Category of the node (Player, Elise, Inventory, …).</summary>
    public InterludeNodeType Type { get; }

    /// <summary>Short display label shown in the UI.</summary>
    public string Label { get; }

    /// <summary>Flavor description shown when the node is focused.</summary>
    public string Description { get; }

    /// <summary>
    /// UI position slot key — a string such as "center", "top", "left", "right",
    /// "bottom-left", "bottom-right". The front-end maps these to absolute positions.
    /// </summary>
    public string PositionSlot { get; }

    /// <summary>Whether the node can be interacted with in the current interlude state.</summary>
    public bool IsEnabled { get; }

    /// <summary>The action triggered when the player selects this node.</summary>
    public InterludeActionKey ActionKey { get; }

    public static InterludeNode Create(
        InterludeNodeType type,
        string label,
        string description,
        string positionSlot,
        bool isEnabled,
        InterludeActionKey actionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(positionSlot);

        return new InterludeNode(
            Guid.NewGuid(),
            type,
            label,
            description,
            positionSlot,
            isEnabled,
            actionKey);
    }
}