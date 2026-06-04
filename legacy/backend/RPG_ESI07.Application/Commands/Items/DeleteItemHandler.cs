using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Items;

public class DeleteItemHandler : IRequestHandler<DeleteItemCommand, DeleteItemResponse>
{
    private readonly IItemRepository _repository;

    public DeleteItemHandler(IItemRepository repository) => _repository = repository;

    public async Task<DeleteItemResponse> Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.Id);
        return new DeleteItemResponse(true, "Item deleted successfully");
    }
}