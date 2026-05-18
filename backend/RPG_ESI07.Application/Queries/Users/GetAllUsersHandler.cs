using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Users;

public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, GetAllUsersResponse>
{
    private readonly IUserRepository _repository;

    public GetAllUsersHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllUsersResponse> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync();

        var dtos = items.Select(u => new UserSummaryDto(
            u.Id,
            u.Username,
            u.Role,
            u.MfaEnabled,
            u.CreatedAt,
            u.LastLoginAt
        )).ToList();

        return new GetAllUsersResponse(dtos);
    }
}