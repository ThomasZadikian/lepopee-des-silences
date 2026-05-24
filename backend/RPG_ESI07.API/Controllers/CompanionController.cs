using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanionController : ControllerBase
{
    private readonly ICompanionRepository _repository;

    public CompanionController(ICompanionRepository repository) => _repository = repository;

    [HttpGet("state")]
    public async Task<IActionResult> GetState()
    {
        var state = await _repository.GetOrCreateAsync();
        return Ok(new
        {
            state.CurrentState,
            state.LastUpdated,
            state.VictoriesToday,
            state.DefeatesToday
        });
    }
}