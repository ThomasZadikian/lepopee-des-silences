using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.BestiaryUnlocks;

public record GetBestiaryUnlockByIdQuery(int Id, int RequestingUserId, bool IsAdmin)
    : IRequest<GetBestiaryUnlockByIdResponse>;

public record GetBestiaryUnlockByIdResponse(BestiaryUnlock BestiaryUnlock);