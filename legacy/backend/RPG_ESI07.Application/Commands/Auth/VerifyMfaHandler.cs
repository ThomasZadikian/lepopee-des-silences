using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Auth;

public class VerifyMfaHandler : IRequestHandler<VerifyMfaCommand, AuthResponse>
{
    private readonly IUserRepository _userRepo;
    private readonly IMfaService _mfaService;
    private readonly ITokenService _tokenService;
    private readonly IEncryptionService _encryption;

    public VerifyMfaHandler(IUserRepository userRepo, IMfaService mfaService, ITokenService tokenService, IEncryptionService encryption)
    {
        _userRepo = userRepo;
        _mfaService = mfaService;
        _tokenService = tokenService;
        _encryption = encryption;
    }

    public async Task<AuthResponse> Handle(VerifyMfaCommand request, CancellationToken ct)
    {
        var userId = _tokenService.ValidateMfaToken(request.MfaToken);
        if (userId == null || userId != request.UserId)
            return new AuthResponse(false, null, false, "Token invalide.");

        var user = await _userRepo.GetByIdAsync(request.UserId);
        if (user == null || user.MfaSecret == null)
            return new AuthResponse(false, null, false, "Identifiants incorrects.");

        var decryptedSecret = _encryption.Decrypt(user.MfaSecret);
        if (!_mfaService.ValidateCode(decryptedSecret, request.Code))
            return new AuthResponse(false, null, false, "Code invalide ou expiré.");

        // Active le MFA si ce n'est pas encore fait
        if (!user.MfaEnabled)
        {
            user.MfaEnabled = true;
            await _userRepo.UpdateAsync(user);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        var token = _tokenService.GenerateAccessToken(user, user.Role);
        return new AuthResponse(true, token, false, "Connexion réussie.");
    }
}