using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Queries.Enemies;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnemiesController : ControllerBase
{
    private readonly IMediator _mediator;
    public EnemiesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
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
}