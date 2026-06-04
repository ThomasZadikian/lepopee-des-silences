using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Items;

public record GetAllItemsQuery : IRequest<GetAllItemsResponse>;

public record GetAllItemsResponse(List<Item> Items);