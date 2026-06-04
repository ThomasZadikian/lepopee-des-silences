using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Skills;

public class GetAllSkillsHandler : IRequestHandler<GetAllSkillsQuery, GetAllSkillsResponse>
{
    private readonly ISkillRepository _repository;

    public GetAllSkillsHandler(ISkillRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllSkillsResponse> Handle(GetAllSkillsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync();
        return new GetAllSkillsResponse(items);
    }
}