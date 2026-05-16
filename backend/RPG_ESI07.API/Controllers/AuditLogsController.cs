using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Commands.AuditLogs;
using RPG_ESI07.Application.Queries.AuditLogs;
using RPG_ESI07.Domain;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllAuditLogsQuery());
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateAuditLogCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteAuditLogCommand(id));
        return Ok(result);
    }
}