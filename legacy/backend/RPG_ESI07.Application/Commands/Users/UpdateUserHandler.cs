using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Users;

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    private readonly IUserRepository _repository;

    public UpdateUserHandler(IUserRepository repository) => _repository = repository;

    public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null) return new UpdateUserResponse(false, "User not found");

        entity.Username = request.Username;
        await _repository.UpdateAsync(entity);
        return new UpdateUserResponse(true, "User updated successfully");
    }
}