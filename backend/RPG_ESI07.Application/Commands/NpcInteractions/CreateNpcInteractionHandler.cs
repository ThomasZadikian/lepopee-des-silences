using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.NpcInteractions;

public class CreateNpcInteractionHandler : IRequestHandler<CreateNpcInteractionCommand, CreateNpcInteractionResponse>
{
    private readonly INpcInteractionRepository _repository;
    public CreateNpcInteractionHandler(INpcInteractionRepository repository) => _repository = repository;

    public async Task<CreateNpcInteractionResponse> Handle(CreateNpcInteractionCommand request, CancellationToken ct)
    {
        var entity = new NpcInteraction
        {
            NpcId = request.NpcId,
            PlayerId = request.PlayerId,
            EventType = request.EventType,
            InteractedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        return new CreateNpcInteractionResponse(entity.Id, "NpcInteraction created successfully");
    }
}