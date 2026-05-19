using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Enemies;

public class DeleteEnemyHandler : IRequestHandler<DeleteEnemyCommand, DeleteEnemyResponse>
{
    private readonly IEnemyRepository _repository;
    public DeleteEnemyHandler(IEnemyRepository repository) => _repository = repository;

    public async Task<DeleteEnemyResponse> Handle(DeleteEnemyCommand request, CancellationToken ct)
    {
        await _repository.DeleteAsync(request.Id);
        return new DeleteEnemyResponse(true, "Enemy deleted successfully");
    }
}