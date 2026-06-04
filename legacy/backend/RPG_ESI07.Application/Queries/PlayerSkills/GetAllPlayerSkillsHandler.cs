using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.PlayerSkills;

public class GetAllPlayerSkillsHandler : IRequestHandler<GetAllPlayerSkillsQuery, GetAllPlayerSkillsResponse>
{
    private readonly IPlayerSkillRepository _repository;

    public GetAllPlayerSkillsHandler(IPlayerSkillRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllPlayerSkillsResponse> Handle(GetAllPlayerSkillsQuery request, CancellationToken cancellationToken)
    {
        var items = request.UserId.HasValue
            ? await _repository.GetByPlayerIdAsync(request.UserId.Value)
            : await _repository.GetAllAsync();

        return new GetAllPlayerSkillsResponse(items);
    }
}