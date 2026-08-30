using MediatR;

namespace Leds.GameEngine.Application.Runs.UseGrimoire;

public sealed record UseGrimoireCommand(
    Guid RunId,
    Guid ItemId,
    Guid CharacterId) : IRequest<UseGrimoireResponse>;

public sealed record UseGrimoireResponse(
    Guid RunId,
    Guid ItemId,
    Guid CharacterId,
    string? GrantedSkillKey,
    int TeamSkillPointsGranted,
    bool ItemDepleted);
