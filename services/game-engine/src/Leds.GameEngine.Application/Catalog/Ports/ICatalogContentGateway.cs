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
}