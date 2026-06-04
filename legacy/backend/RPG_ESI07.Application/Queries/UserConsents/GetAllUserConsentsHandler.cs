using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.UserConsents;

public class GetAllUserConsentsHandler : IRequestHandler<GetAllUserConsentsQuery, GetAllUserConsentsResponse>
{
    private readonly IUserConsentRepository _repository;

    public GetAllUserConsentsHandler(IUserConsentRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllUserConsentsResponse> Handle(GetAllUserConsentsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync();
        return new GetAllUserConsentsResponse(items);
    }
}