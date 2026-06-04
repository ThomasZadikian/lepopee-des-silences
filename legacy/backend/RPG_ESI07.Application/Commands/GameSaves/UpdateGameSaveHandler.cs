using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.GameSaves;

public class UpdateGameSaveHandler : IRequestHandler<UpdateGameSaveCommand, UpdateGameSaveResponse>
{
    private readonly IGameSaveRepository _repository;

    public UpdateGameSaveHandler(IGameSaveRepository repository) => _repository = repository;

    public async Task<UpdateGameSaveResponse> Handle(UpdateGameSaveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("GameSave not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        entity.PlayerId = request.PlayerId;
        entity.CurrentZone = request.CurrentZone;
        await _repository.UpdateAsync(entity);
        return new UpdateGameSaveResponse(true, "GameSave updated successfully");
    }
}