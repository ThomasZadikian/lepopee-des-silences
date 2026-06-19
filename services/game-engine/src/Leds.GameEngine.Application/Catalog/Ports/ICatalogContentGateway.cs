using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.SharedBuildingBlocks.Results;

namespace Leds.GameEngine.Application.Catalog.Ports;

/// <summary>
/// Application port used by the Game Engine to access versioned Catalog content.
/// Implementations belong to Infrastructure.
/// </summary>
public interface ICatalogContentGateway
{
    Task<Result<EnemyTemplateSnapshot>> GetEnemyTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<Result<SkillTemplateSnapshot>> GetSkillTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<Result<ItemTemplateSnapshot>> GetItemTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<Result<EventTemplateSnapshot>> GetEventTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<Result<PalaceLawDefinitionSnapshot>> GetPalaceLawDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PalaceLawDefinitionSnapshot>> ListActivePalaceLawDefinitionsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<CatalogCurseDefinitionSnapshot>> GetCurseDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogCurseDefinitionSnapshot>> ListAvailableCurseDefinitionsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<CatalogItemDefinitionSnapshot>> GetItemDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<Result<CatalogEffectSetSnapshot>> GetEffectSetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<Result<CatalogRewardTemplateSnapshot>> GetRewardTemplateByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogRewardTemplateSnapshot>> ListEligibleRewardTemplatesAsync(
        RewardTemplateEligibilityContext context,
        CancellationToken cancellationToken = default);

    Task<CatalogRoomBossProfile?> GetRoomBossProfileAsync(
        string roomType,
        CancellationToken cancellationToken = default);

    Task<CatalogEnemyDefinition?> GetEnemyDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogEnemyDefinition>> ListEnemyDefinitionsByRoomTypeAsync(
        string roomType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogEnemyDefinition>> ListCompatibleEnemyDefinitionsAsync(
        string roomType,
        int riskLevel,
        CancellationToken cancellationToken = default);

    Task<CatalogSkillDefinition?> GetSkillDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogSkillDefinition>> ListSkillDefinitionsByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogSkillDefinition>> ListSkillDefinitionsByTypeAsync(
        string skillType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogNpcDefinition>> ListNpcDefinitionsAsync(
        CancellationToken cancellationToken = default);
}
