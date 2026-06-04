using MediatR;

namespace RPG_ESI07.Application.Commands.Items;

public record CreateItemCommand(
    string Name,
    string Type,
    int Price,
    string? Category,
    string? Description,
    int? EffectValue,
    string? StatModifiers
) : IRequest<CreateItemResponse>;

public record CreateItemResponse(int Id, string Message);