namespace Leds.GameEngine.Domain.Protocol;

public sealed record LocalRuleEvaluationResult
{
    private static readonly LocalRuleEvaluationResult NotApplicableResult = new(
        LocalRuleEvaluationOutcome.NotApplicable, message: null, []);

    private LocalRuleEvaluationResult(
        LocalRuleEvaluationOutcome outcome,
        string? message,
        IReadOnlyList<LocalRuleConsequence> newConsequences)
    {
        Outcome = outcome;
        Message = message;
        NewConsequences = newConsequences;
    }

    public LocalRuleEvaluationOutcome Outcome { get; }

    /// <summary>The rule's InfoMessage (Informed) or WarningMessage (Transgression's first
    /// occurrence); null once the warning has already been shown once, and always null for
    /// NotApplicable.</summary>
    public string? Message { get; }

    /// <summary>Consequences newly unlocked by this call — always empty outside Transgression.</summary>
    public IReadOnlyList<LocalRuleConsequence> NewConsequences { get; }

    public static LocalRuleEvaluationResult NotApplicable() => NotApplicableResult;

    public static LocalRuleEvaluationResult Informed(string infoMessage) =>
        new(LocalRuleEvaluationOutcome.Informed, infoMessage, []);

    public static LocalRuleEvaluationResult Transgression(
        string? warningMessage, IReadOnlyList<LocalRuleConsequence> newConsequences) =>
        new(LocalRuleEvaluationOutcome.Transgression, warningMessage, newConsequences);
}
