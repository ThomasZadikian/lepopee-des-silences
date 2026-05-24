using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Commands.Npcs;
using RPG_ESI07.Application.Queries.Npcs;
using RPG_ESI07.Domain;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NpcsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NpcsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllNpcsQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetNpcByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("zone/{zone}")]
    public async Task<IActionResult> GetByZone(string zone)
    {
        var result = await _mediator.Send(new GetNpcsByZoneQuery(zone));
        return Ok(result);
    }

    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetByType(string type)
    {
        var result = await _mediator.Send(new GetNpcsByTypeQuery(type));
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateNpcCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateNpcCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteNpcCommand(id));
        return Ok(result);
    }
}