using Leds.GameEngine.Domain.Markov;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.States;

public sealed class MarkovPalaceRoomStateResolver : IPalaceRoomStateResolver
{
    private const string MatrixKey = "palace-room-state-transition";
    private const string Scope = "palace-room-state-generation";

    private readonly MarkovTransitionResolver _transitionResolver;

    public MarkovPalaceRoomStateResolver(MarkovTransitionResolver transitionResolver)
    {
        _transitionResolver = transitionResolver;
    }

    public PalaceRoomState ResolveNextState(PalaceRoomStateResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Seed))
        {
            throw new ArgumentException("Seed is required.", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.MatrixVersion))
        {
            throw new ArgumentException("Matrix version is required.", nameof(context));
        }

        if (!string.Equals(
                context.MatrixVersion,
                StaticRoomTypeMarkovMatrixProvider.SupportedVersion,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported palace room state matrix version '{context.MatrixVersion}'.");
        }

        if (context.NextRoomDepth <= 0 || context.NextRoomType == RoomType.Threshold)
        {
            return PalaceRoomState.Neutral;
        }

        if (context.NextRoomType == RoomType.Final)
        {
            return PalaceRoomState.Neutral;
        }

        var matrix = CreateMatrix(context.MatrixVersion);
        var currentState = ToCandidateState(context.PreviousRoomState);
        var scope = BuildScope(context);

        var nextState = _transitionResolver.ResolveNextState(
            matrix,
            MarkovState.Create(currentState.ToString()),
            context.Seed,
            scope,
            context.NextRoomDepth);

        return Enum.Parse<PalaceRoomState>(nextState.Value);
    }

    private static PalaceRoomState ToCandidateState(PalaceRoomState state)
    {
        return state is PalaceRoomState.Silent or PalaceRoomState.Painful
            ? state
            : PalaceRoomState.Neutral;
    }

    private static MarkovTransitionMatrix CreateMatrix(string version)
    {
        var neutral = MarkovState.Create(PalaceRoomState.Neutral.ToString());
        var silent = MarkovState.Create(PalaceRoomState.Silent.ToString());
        var painful = MarkovState.Create(PalaceRoomState.Painful.ToString());

        var states = new[] { neutral, silent, painful };

        return MarkovTransitionMatrix.Create(
            MatrixKey,
            version,
            states,
            [
                MarkovTransitionRow.Create(
                    neutral,
                    new Dictionary<MarkovState, decimal>
                    {
                        [neutral] = 0.50m,
                        [silent] = 0.25m,
                        [painful] = 0.25m
                    }),
                MarkovTransitionRow.Create(
                    silent,
                    new Dictionary<MarkovState, decimal>
                    {
                        [neutral] = 0.35m,
                        [silent] = 0.45m,
                        [painful] = 0.20m
                    }),
                MarkovTransitionRow.Create(
                    painful,
                    new Dictionary<MarkovState, decimal>
                    {
                        [neutral] = 0.35m,
                        [silent] = 0.20m,
                        [painful] = 0.45m
                    })
            ]);
    }

    private static string BuildScope(PalaceRoomStateResolutionContext context)
    {
        var lawKeys = string.Join(',', (context.ActiveLawKeys ?? [])
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Order(StringComparer.OrdinalIgnoreCase));
        var curseKeys = string.Join(',', (context.ActiveCurseKeys ?? [])
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Order(StringComparer.OrdinalIgnoreCase));

        return string.Join(
            '|',
            Scope,
            context.PreviousRoomType.ToString(),
            context.NextRoomType.ToString(),
            context.NextRoomDepth.ToString(),
            lawKeys,
            curseKeys,
            context.ActiveClimate?.Trim() ?? string.Empty);
    }
}
