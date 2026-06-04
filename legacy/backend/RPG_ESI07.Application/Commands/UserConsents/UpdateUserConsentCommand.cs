using MediatR;

namespace RPG_ESI07.Application.Commands.UserConsents;

public record UpdateUserConsentCommand(
    int Id, int UserId, bool AnalyticsConsent, bool MarketingConsent,
    int RequestingUserId, bool IsAdmin
) : IRequest<UpdateUserConsentResponse>;

public record UpdateUserConsentResponse(bool Success, string Message);