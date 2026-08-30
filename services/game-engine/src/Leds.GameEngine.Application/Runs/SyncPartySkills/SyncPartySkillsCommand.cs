using MediatR;

namespace Leds.GameEngine.Application.Runs.SyncPartySkills;

public sealed record SyncPartySkillsCommand(Guid RunId) : IRequest<SyncPartySkillsResponse>;
