using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Npcs;
public class DeleteNpcHandler : IRequestHandler<DeleteNpcCommand, DeleteNpcResponse>
{
    private readonly INpcRepository _repository;
    public DeleteNpcHandler(INpcRepository repository) => _repository = repository;

    public async Task<DeleteNpcResponse> Handle(DeleteNpcCommand request, CancellationToken ct)
    {
        await _repository.DeleteAsync(request.Id);
        return new DeleteNpcResponse(true, "Npc deleted successfully");
    }
}