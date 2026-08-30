using Leds.Catalog.Domain.Gameplay;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Catalog.Api.Controllers;

[ApiController]
[Route("api/v2/catalog/character-combat-definitions")]
public sealed class CharacterCombatDefinitionsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ListActive() => Ok(new
    {
        Definitions = CharacterCombatDefinitionCatalog.All.Select(definition => new
        {
            definition.DefinitionKey,
            Kind = definition.Kind.ToString(),
            definition.CombatArchetypeCode,
            EmotionalRegister = EmotionalRegisterCatalog.CodeOf(definition.EmotionalRegister)
        })
    });
}
