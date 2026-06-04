using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.NpcInteractions;

public class DeleteNpcInteractionHandler : IRequestHandler<DeleteNpcInteractionCommand, DeleteNpcInteractionResponse>
{
    private readonly INpcInteractionRepository _repository;
    public DeleteNpcInteractionHandler(INpcInteractionRepository repository) => _repository = repository;

    public async Task<DeleteNpcInteractionResponse> Handle(DeleteNpcInteractionCommand request, CancellationToken ct)
    {
        await _repository.DeleteAsync(request.Id);
        return new DeleteNpcInteractionResponse(true, "NpcInteraction deleted successfully");
    }
}