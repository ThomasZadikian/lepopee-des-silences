using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.RGPD;

public class AnonymizeUserHandler : IRequestHandler<AnonymizeUserCommand, AnonymizeUserResponse>
{
    private readonly IUserRepository _repository;

    public AnonymizeUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<AnonymizeUserResponse> Handle(AnonymizeUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException("Utilisateur introuvable");
        if (user.DeletedAt != null)
            return new AnonymizeUserResponse(false, "Compte deja supprime");
        user.Username = $"deleted_user_{user.Id}";
        user.Email = Array.Empty<byte>();
        user.PasswordHash = string.Empty;
        user.MfaSecret = null;
        user.MfaEnabled = false;
        user.LastLoginIP = null;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletionReason = request.Reason ?? "Demande utilisateur (Art. 17 RGPD)";
        await _repository.UpdateAsync(user);
        return new AnonymizeUserResponse(true, "Compte anonymise avec succes conformement a l'Art. 17 du RGPD");
    }
}