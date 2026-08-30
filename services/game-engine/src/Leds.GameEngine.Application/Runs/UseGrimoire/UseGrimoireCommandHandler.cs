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

        var itemDefinitionResult = await _catalog.GetItemDefinitionByKeyAsync(
            item.DefinitionKey, cancellationToken);
        if (!itemDefinitionResult.IsSuccess)
            throw new DomainException($"Grimoire '{item.DefinitionKey}' is missing from Catalog.");

        var itemDefinition = itemDefinitionResult.Value;
        var skillKey = (itemDefinition.EquipmentEffects ?? [])
            .SingleOrDefault(effect => string.Equals(
                effect.Kind, "GrantSkill", StringComparison.OrdinalIgnoreCase))
            ?.SkillKey;
        if (string.IsNullOrWhiteSpace(skillKey))
            throw new DomainException($"Grimoire '{item.DefinitionKey}' has no authored temporary skill.");

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
            skill.Cooldown, skill.IsUltimate, skill.EmotionalRegister,
            temporarySlot: "Grimoire");
}
