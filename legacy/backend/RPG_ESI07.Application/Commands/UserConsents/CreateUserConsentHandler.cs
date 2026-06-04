using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.UserConsents;

public class CreateUserConsentHandler : IRequestHandler<CreateUserConsentCommand, CreateUserConsentResponse>
{
    private readonly IUserConsentRepository _repository;

    public CreateUserConsentHandler(IUserConsentRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateUserConsentResponse> Handle(CreateUserConsentCommand request, CancellationToken cancellationToken)
    {
        var entity = new UserConsent();
        await _repository.AddAsync(entity);
        return new CreateUserConsentResponse(entity.Id, "UserConsent created successfully");
    }
}