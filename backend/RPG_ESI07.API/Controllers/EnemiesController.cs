using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Commands.Enemies;
using RPG_ESI07.Application.Queries.Enemies;
using RPG_ESI07.Domain;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnemiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnemiesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllEnemiesQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetEnemyByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetByType(string type)
    {
        var result = await _mediator.Send(new GetEnemiesByTypeQuery(type));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateEnemyCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEnemyCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteEnemyCommand(id));
        return Ok(result);
    }

    [HttpGet("{id}/scaled")]
    [Authorize]
    public async Task<IActionResult> GetScaled(int id, [FromQuery] int playerLevel, [FromQuery] int equipmentBonus = 0)
    {
        if (playerLevel < 1 || playerLevel > 99) return BadRequest("Niveau invalide.");
        if (equipmentBonus < 0 || equipmentBonus > 500) return BadRequest("Bonus équipement invalide.");

        var result = await _mediator.Send(new GetScaledEnemyQuery(id, playerLevel, equipmentBonus));
        if (result.Enemy == null) return NotFound();
        return Ok(result);
    }
}