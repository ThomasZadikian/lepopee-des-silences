using MediatR;

namespace RPG_ESI07.Application.Commands.UserConsents;

public record CreateUserConsentCommand(
    int UserId, bool AnalyticsConsent, bool MarketingConsent
) : IRequest<CreateUserConsentResponse>;

public record CreateUserConsentResponse(int Id, string Message);