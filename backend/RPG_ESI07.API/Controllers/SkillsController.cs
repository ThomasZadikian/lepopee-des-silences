using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Commands.Skills;
using RPG_ESI07.Application.Queries.Skills;
using RPG_ESI07.Domain;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors("AllowFrontend")]
public class SkillsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SkillsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllSkillsQuery());
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateSkillCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSkillCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch");
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteSkillCommand(id));
        return Ok(result);
    }
}