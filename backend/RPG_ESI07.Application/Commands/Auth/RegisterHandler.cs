using MediatR;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Auth;

public class RegisterHandler
: IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;

    public RegisterHandler(
    IUserRepository userRepo,
    IPasswordHasher hasher,
    ITokenService tokenService)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(
    RegisterCommand request,
    CancellationToken cancellationToken)
    {
        // 1. Vérifier unicité username
        if (await _userRepo.UsernameExistsAsync(
        request.Username))
            return new AuthResponse(false, null, false,
            "Username already exists");
        // 2. Hash du password
        var hash = _hasher.HashPassword(
            request.Password);
        // 3. Créer le User
        var user = new User
        {
            Username = request.Username,
            Email = System.Text.Encoding
        .UTF8.GetBytes(request.Email),
            PasswordHash = hash,
            MfaEnabled = false
        };
        await _userRepo.AddAsync(user);
        // 4. Générer le JWT
        var token = _tokenService
        .GenerateAccessToken(user, Constants.RolePlayer);
        return new AuthResponse(true, token, false,
        "Registration successful");
    }
}