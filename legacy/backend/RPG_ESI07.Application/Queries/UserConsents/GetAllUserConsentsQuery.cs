using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.UserConsents;

public record GetAllUserConsentsQuery : IRequest<GetAllUserConsentsResponse>;

public record GetAllUserConsentsResponse(List<UserConsent> Items);