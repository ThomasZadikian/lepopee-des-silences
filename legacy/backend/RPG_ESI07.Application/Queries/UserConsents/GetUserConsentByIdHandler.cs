using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.UserConsents;

public class GetUserConsentByIdHandler : IRequestHandler<GetUserConsentByIdQuery, GetUserConsentByIdResponse>
{
    private readonly IUserConsentRepository _repository;

    public GetUserConsentByIdHandler(IUserConsentRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetUserConsentByIdResponse> Handle(GetUserConsentByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("UserConsent not found");

        // UserConsent utilise UserId (pas PlayerId)
        if (!request.IsAdmin && entity.UserId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        return new GetUserConsentByIdResponse(entity);
    }
}