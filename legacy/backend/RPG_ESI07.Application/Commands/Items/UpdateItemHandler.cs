using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Items;

public class UpdateItemHandler : IRequestHandler<UpdateItemCommand, UpdateItemResponse>
{
    private readonly IItemRepository _repository;

    public UpdateItemHandler(IItemRepository repository) => _repository = repository;

    public async Task<UpdateItemResponse> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null) return new UpdateItemResponse(false, "Item not found");

        entity.Name = request.Name;
        entity.Type = request.Type;
        entity.Price = request.Price;
        entity.Category = request.Category;
        entity.Description = request.Description;
        entity.EffectValue = request.EffectValue;
        entity.StatModifiers = string.IsNullOrWhiteSpace(request.StatModifiers) ? null : request.StatModifiers;

        await _repository.UpdateAsync(entity);
        return new UpdateItemResponse(true, "Item updated successfully");
    }
}