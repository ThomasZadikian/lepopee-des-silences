using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.GetRunReputation;

public sealed class GetRunReputationQueryHandler
    : IRequestHandler<GetRunReputationQuery, GetRunReputationResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly ICatalogContentGateway _catalogContentGateway;

    public GetRunReputationQueryHandler(
        IRunRepository runRepository,
        ICatalogContentGateway catalogContentGateway)
    {
        _runRepository = runRepository;
        _catalogContentGateway = catalogContentGateway;
    }

    public async Task<GetRunReputationResponse> Handle(
        GetRunReputationQuery request,
        CancellationToken cancellationToken)
    {
        var runId = new RunId(request.RunId);

        var run = await _runRepository.GetByIdAsync(runId, cancellationToken);

        if (run is null)
        {
            throw new NotFoundException("Run", request.RunId);
        }

        if (run.NpcRelationships.Count == 0)
        {
            return new GetRunReputationResponse(run.Id.Value, []);
        }

        var npcDefinitions = await _catalogContentGateway.ListNpcDefinitionsAsync(cancellationToken);
        var npcsByKey = npcDefinitions.ToDictionary(n => n.Key, StringComparer.OrdinalIgnoreCase);

        var npcs = run.NpcRelationships
            .Where(relationship => npcsByKey.ContainsKey(relationship.NpcKey))
            .Select(relationship => ToDto(relationship, npcsByKey[relationship.NpcKey]))
            .OrderByDescending(dto => dto.RelationshipScore)
            .ToArray();

        return new GetRunReputationResponse(run.Id.Value, npcs);
    }

    private static NpcReputationDto ToDto(NpcRelationship relationship, CatalogNpcDefinition npc)
    {
        var offerings = (npc.Offerings ?? [])
            .Select(offering => ToOfferingDto(offering, relationship))
            .ToArray();

        return new NpcReputationDto(
            npc.Key,
            npc.DisplayName,
            npc.EmotionalAffinity,
            relationship.RelationshipScore,
            relationship.AggregateState.ToString(),
            relationship.TimesMet,
            offerings);
    }

    private static NpcOfferingReputationDto ToOfferingDto(CatalogNpcOffering offering, NpcRelationship relationship)
    {
        var requiredScore = offering.UnlockConditions
            .FirstOrDefault(c => string.Equals(c.Kind, "RelationshipScoreAtLeast", StringComparison.OrdinalIgnoreCase))
            ?.RequiredRelationshipScore;

        var scoreThresholdMet = requiredScore is null || relationship.RelationshipScore >= requiredScore;

        return new NpcOfferingReputationDto(
            offering.Key,
            offering.Kind,
            offering.IsMajor,
            requiredScore,
            scoreThresholdMet);
    }
}
