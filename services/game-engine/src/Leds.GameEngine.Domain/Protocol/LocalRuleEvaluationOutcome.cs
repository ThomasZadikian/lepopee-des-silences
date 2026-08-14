namespace Leds.GameEngine.Domain.Protocol;

public enum LocalRuleEvaluationOutcome
{
    /// <summary>The trigger context doesn't meet this rule's condition — nothing happened.</summary>
    NotApplicable = 0,

    /// <summary>First contact with the condition: the party is informed, no severity accrues.</summary>
    Informed = 1,

    /// <summary>The condition was met again after the party was already informed — a
    /// transgression, severity increased, zero or more new consequences returned.</summary>
    Transgression = 2,
}
