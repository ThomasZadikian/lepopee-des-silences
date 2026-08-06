using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed record ListBossCodexQuery : IRequest<ListBossCodexResponse>;

public sealed record BossCodexEntry(
    string Key,
    string DisplayName,
    string Description,
    string EmotionalRegister,
    IReadOnlyCollection<string> CompatibleRoomTypes,
    int Threat);

public sealed record ListBossCodexResponse(IReadOnlyCollection<BossCodexEntry> Bosses);
