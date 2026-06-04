using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.GameSaves;

public record GetSaveByIdQuery(int Id, int RequestingUserId, bool IsAdmin)
    : IRequest<GetSaveByIdResponse>;

public record GetSaveByIdResponse(GameSave GameSave);