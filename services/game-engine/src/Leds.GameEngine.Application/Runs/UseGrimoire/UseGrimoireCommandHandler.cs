using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.UseGrimoire;

public sealed class UseGrimoireCommandHandler
    : IRequestHandler<UseGrimoireCommand, UseGrimoireResponse>
{
    private static readonly IReadOnlyDictionary<string, string> SkillByGrimoire =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["item.tome-marees"] = "skill.temp.deluge-mineur",
            ["item.feuillet-copiste"] = "skill.temp.ecriture-appliquee",
            ["item.braise-volee"] = "skill.temp.souffle-emprunte",
            ["item.retable-portatif"] = "canon.skill.priere-aspiration",
            ["item.carnet-croquis"] = "skill.temp.construction-ephemere"
        };

    private readonly IRunRepository _runs;
    private readonly ICatalogContentGateway _catalog;
    private readonly IPlayerProfileGateway _players;

    public UseGrimoireCommandHandler(
        IRunRepository runs,
        ICatalogContentGateway catalog,
        IPlayerProfileGateway players)
    {
        _runs = runs;
        _catalog = catalog;
        _players = players;
    }

    public async Task<UseGrimoireResponse> Handle(
        UseGrimoireCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runs.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);
        var item = run.RunItems.FirstOrDefault(candidate => candidate.Id.Value == request.ItemId)
            ?? throw new NotFoundException("Run item", request.ItemId);
        var character = run.PlayerSnapshot?.Characters
            .FirstOrDefault(candidate => candidate.CharacterId == request.CharacterId)
            ?? throw new DomainException($"Character '{request.CharacterId}' was not found in this run.");

        if (!SkillByGrimoire.TryGetValue(item.DefinitionKey, out var skillKey))
            throw new DomainException($"Grimoire '{item.DefinitionKey}' has no authored temporary skill.");

        if (item.DefinitionKey.Equals("item.braise-volee", StringComparison.OrdinalIgnoreCase)
            && character.Skills.Any(skill =>
                skill.SkillDefinitionKey.Equals(
                    "canon.skill.souffle-de-la-forge",
                    StringComparison.OrdinalIgnoreCase)))
        {
            var depletedForPoints = run.ConsumeGrimoire(item.Id);
            await _players.AwardStatPointsAsync(run.PlayerId, 8, cancellationToken);
            await _runs.UpdateAsync(run, cancellationToken);
            return new(
                run.Id.Value, item.Id.Value, request.CharacterId,
                GrantedSkillKey: null,
                TeamSkillPointsGranted: 8,
                ItemDepleted: depletedForPoints);
        }

        var skill = await _catalog.GetSkillDefinitionByKeyAsync(skillKey, cancellationToken)
            ?? throw new DomainException($"Temporary skill '{skillKey}' is missing from the catalog.");
        var runtimeSkill = ToRuntimeSkill(skill);
        var snapshotSkill = ToSnapshotSkill(skill);
        var depleted = run.GrantGrimoireSkill(
            item.Id, request.CharacterId, runtimeSkill, snapshotSkill);

        await _runs.UpdateAsync(run, cancellationToken);
        return new(
            run.Id.Value, item.Id.Value, request.CharacterId,
            GrantedSkillKey: skill.Key,
            TeamSkillPointsGranted: 0,
            ItemDepleted: depleted);
    }

    private static PlayerRuntimeSkill ToRuntimeSkill(CatalogSkillDefinition skill) =>
        PlayerRuntimeSkill.Create(
            skill.Key, skill.DisplayName, skill.SkillType, skill.TargetingType,
            skill.EffectType, skill.ManaCost, skill.ChargeCost, skill.BasePower,
            skill.Category, skill.BasePowerIsPercentOfMaxVitality,
            skill.TacticalRange, skill.TacticalAreaShape, skill.RequiresLineOfSight,
            skill.Cooldown, skill.IsUltimate, skill.EmotionalRegister);

    private static RunCharacterSkillSnapshot ToSnapshotSkill(CatalogSkillDefinition skill) =>
        RunCharacterSkillSnapshot.Create(
            skill.Key, skill.DisplayName, skill.SkillType, skill.TargetingType,
            skill.EffectType, skill.ManaCost, skill.ChargeCost, skill.BasePower,
            skill.Category, skill.BasePowerIsPercentOfMaxVitality,
            skill.TacticalRange, skill.TacticalAreaShape, skill.RequiresLineOfSight,
            skill.Cooldown, skill.IsUltimate, skill.EmotionalRegister);
}
