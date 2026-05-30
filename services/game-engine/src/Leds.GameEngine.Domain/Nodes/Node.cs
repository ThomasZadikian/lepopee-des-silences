using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Nodes;

public sealed class Node
{
    private Node(
        NodeId id,
        NodeEventType eventType,
        int riskLevel,
        string rewardProfile,
        NodeState state)
    {
        Id = id;
        EventType = eventType;
        RiskLevel = riskLevel;
        RewardProfile = rewardProfile;
        State = state;
    }

    public NodeId Id { get; }

    public NodeEventType EventType { get; }

    public int RiskLevel { get; }

    public string RewardProfile { get; }

    public NodeState State { get; private set; }

    public bool IsAvailable => State == NodeState.Available;

    public static Node Create(
        NodeEventType eventType,
        int riskLevel,
        string rewardProfile)
    {
        if (riskLevel is < 0 or > 100)
        {
            throw new DomainException("Node risk level must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(rewardProfile))
        {
            throw new DomainException("Node reward profile is required.");
        }

        return new Node(
            NodeId.New(),
            eventType,
            riskLevel,
            rewardProfile.Trim(),
            NodeState.Available);
    }

    public void Select()
    {
        if (State != NodeState.Available)
        {
            throw new DomainException("Only an available node can be selected.");
        }

        State = NodeState.Selected;
    }

    public void Lock()
    {
        if (State == NodeState.Selected || State == NodeState.Resolved)
        {
            return;
        }

        State = NodeState.Locked;
    }

    public void Resolve()
    {
        if (State != NodeState.Selected)
        {
            throw new DomainException("Only a selected node can be resolved.");
        }

        State = NodeState.Resolved;
    }
}