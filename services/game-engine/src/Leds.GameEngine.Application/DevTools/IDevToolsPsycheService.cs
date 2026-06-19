namespace Leds.GameEngine.Application.DevTools;

public interface IDevToolsPsycheService
{
    Task<DevToolsRunPsycheResult> GetPsycheAsync(Guid runId, CancellationToken cancellationToken = default);
}

/// <summary>État courant de la psyché + sa trajectoire salle par salle (probabilités par émotion).</summary>
public sealed record DevToolsRunPsycheResult(
    Guid RunId,
    string Dominant,
    IReadOnlyDictionary<string, decimal> Current,
    IReadOnlyList<DevToolsPsycheStep> Trajectory);

public sealed record DevToolsPsycheStep(
    int Depth,
    string Dominant,
    IReadOnlyDictionary<string, decimal> Distribution);