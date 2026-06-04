using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.UserConsents;

public class UpdateUserConsentHandler : IRequestHandler<UpdateUserConsentCommand, UpdateUserConsentResponse>
{
    private readonly IUserConsentRepository _repository;

    public UpdateUserConsentHandler(IUserConsentRepository repository) => _repository = repository;

    public async Task<UpdateUserConsentResponse> Handle(UpdateUserConsentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("UserConsent not found");

        if (!request.IsAdmin && entity.UserId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        entity.UserId = request.UserId;
        entity.AnalyticsConsent = request.AnalyticsConsent;
        entity.MarketingConsent = request.MarketingConsent;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(entity);
        return new UpdateUserConsentResponse(true, "UserConsent updated successfully");
    }
}