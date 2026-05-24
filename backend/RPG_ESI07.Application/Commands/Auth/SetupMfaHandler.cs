using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Auth;

public class SetupMfaHandler : IRequestHandler<SetupMfaCommand, SetupMfaResponse>
{
    private readonly IUserRepository _userRepo;
    private readonly IMfaService _mfaService;
    private readonly ITokenService _tokenService;

    public SetupMfaHandler(IUserRepository userRepo, IMfaService mfaService, ITokenService tokenService)
    {
        _userRepo = userRepo;
        _mfaService = mfaService;
        _tokenService = tokenService;
    }

    public async Task<SetupMfaResponse> Handle(SetupMfaCommand request, CancellationToken ct)
    {
        var userId = _tokenService.ValidateMfaToken(request.MfaToken);
        if (userId == null || userId != request.UserId)
            return new SetupMfaResponse(false, null, null, "Token invalide.");

        var user = await _userRepo.GetByIdAsync(request.UserId);
        if (user == null)
            return new SetupMfaResponse(false, null, null, "Utilisateur introuvable.");

        // Générer le secret TOTP — byte[] directement
        var secret = _mfaService.GenerateSecret();
        var qrCodeUri = _mfaService.GetQrCodeUri(
            _mfaService.SecretToBase32(secret),
            user.Username);

        // Stocker le secret
        user.MfaSecret = secret;
        await _userRepo.UpdateAsync(user);

        return new SetupMfaResponse(
            true,
            qrCodeUri,
            _mfaService.SecretToBase32(secret),
            "Scannez le QR code avec votre application TOTP.");
    }
}