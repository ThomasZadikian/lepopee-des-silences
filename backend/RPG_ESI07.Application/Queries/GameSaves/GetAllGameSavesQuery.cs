using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.GameSaves;

public record GetAllGameSavesQuery(int? UserId = null) : IRequest<GetAllGameSavesResponse>;
public record GetAllGameSavesResponse(List<GameSave> Items);