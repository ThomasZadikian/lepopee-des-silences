using MediatR;

namespace RPG_ESI07.Application.Commands.Items;

public record UpdateItemCommand(
    int Id,
    string Name,
    string Type,
    int Price,
    string? Category,
    string? Description,
    int? EffectValue,
    string? StatModifiers
) : IRequest<UpdateItemResponse>;

public record UpdateItemResponse(bool Success, string Message);