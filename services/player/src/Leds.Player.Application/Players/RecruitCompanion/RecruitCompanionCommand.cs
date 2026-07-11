using MediatR;

namespace Leds.Player.Application.Players.RecruitCompanion;

public sealed record RecruitCompanionCommand(
    Guid PlayerId,
    string CompanionDefinitionKey,
    string DisplayName,
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Recovery,
    int Focus,
    int Mana,
    int Charge,
    IReadOnlyCollection<string> SkillKeys) : IRequest<PlayerProfileDto>;
