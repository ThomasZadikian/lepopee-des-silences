using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.PlayerSkills;

public class GetPlayerSkillByIdHandler : IRequestHandler<GetPlayerSkillByIdQuery, GetPlayerSkillByIdResponse>
{
    private readonly IPlayerSkillRepository _repository;

    public GetPlayerSkillByIdHandler(IPlayerSkillRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPlayerSkillByIdResponse> Handle(GetPlayerSkillByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("PlayerSkill not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        return new GetPlayerSkillByIdResponse(entity);
    }
}