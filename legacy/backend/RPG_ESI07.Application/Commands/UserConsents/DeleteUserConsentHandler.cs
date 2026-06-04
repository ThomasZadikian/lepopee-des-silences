using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.UserConsents;

public class DeleteUserConsentHandler : IRequestHandler<DeleteUserConsentCommand, DeleteUserConsentResponse>
{
    private readonly IUserConsentRepository _repository;

    public DeleteUserConsentHandler(IUserConsentRepository repository) => _repository = repository;

    public async Task<DeleteUserConsentResponse> Handle(DeleteUserConsentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("UserConsent not found");

        if (!request.IsAdmin && entity.UserId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        await _repository.DeleteAsync(request.Id);
        return new DeleteUserConsentResponse(true, "UserConsent deleted successfully");
    }
}