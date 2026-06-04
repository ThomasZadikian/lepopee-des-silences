using MediatR;
using Microsoft.EntityFrameworkCore;
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
    private readonly IPlayerProfileRepository _profileRepo;

    public RegisterHandler(
    IUserRepository userRepo,
    IPasswordHasher hasher,
    ITokenService tokenService, 
    IPlayerProfileRepository profileRepo)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _tokenService = tokenService;
        _profileRepo = profileRepo; 
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepo.UsernameExistsAsync(request.Username))
            return new AuthResponse(false, null, false, "Identifiants incorrects.");

        var hash = _hasher.HashPassword(request.Password);
        var user = new User
        {
            Username = request.Username,
            Email = System.Text.Encoding.UTF8.GetBytes(request.Email),
            PasswordHash = hash,
            MfaEnabled = false
        };

        try
        {
            user = await _userRepo.AddAsync(user);
        }
        catch (DbUpdateException)
        {
            return new AuthResponse(false, null, false, "Identifiants incorrects.");
        }

        var profile = new PlayerProfile
        {
            UserId = user.Id,
            CharacterName = request.Username,
            Level = 1,
            Experience = 0,
            Gold = 0,
            UpdatedAt = DateTime.UtcNow,
        };

        await _profileRepo.AddAsync(profile);


        var mfaSetupToken = _tokenService.GenerateMfaToken(user);
        return new AuthResponse(true, mfaSetupToken, false, "Registration successful. MFA setup required.", requiresMfaSetup: true, userId: user.Id);
    }
}