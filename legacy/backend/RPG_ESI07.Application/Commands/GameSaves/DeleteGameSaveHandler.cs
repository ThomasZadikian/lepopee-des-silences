using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.GameSaves;

public class DeleteGameSaveHandler : IRequestHandler<DeleteGameSaveCommand, DeleteGameSaveResponse>
{
    private readonly IGameSaveRepository _repository;

    public DeleteGameSaveHandler(IGameSaveRepository repository) => _repository = repository;

    public async Task<DeleteGameSaveResponse> Handle(DeleteGameSaveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("GameSave not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        await _repository.DeleteAsync(request.Id);
        return new DeleteGameSaveResponse(true, "GameSave deleted successfully");
    }
}