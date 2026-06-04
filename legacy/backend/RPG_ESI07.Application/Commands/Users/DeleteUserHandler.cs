using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Users;

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, DeleteUserResponse>
{
    private readonly IUserRepository _repository;

    public DeleteUserHandler(IUserRepository repository) => _repository = repository;

    public async Task<DeleteUserResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null) return new DeleteUserResponse(false, "User not found");

        await _repository.DeleteAsync(entity);
        return new DeleteUserResponse(true, "User deleted successfully");
    }
}