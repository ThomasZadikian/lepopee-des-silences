using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.UserConsents;

public record GetUserConsentByIdQuery(int Id, int RequestingUserId, bool IsAdmin)
    : IRequest<GetUserConsentByIdResponse>;

public record GetUserConsentByIdResponse(UserConsent UserConsent);