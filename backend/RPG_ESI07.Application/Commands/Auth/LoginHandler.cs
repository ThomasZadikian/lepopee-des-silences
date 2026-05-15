using MediatR;
using Microsoft.Extensions.Logging;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Auth;

public class LoginHandler
: IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
    IUserRepository userRepo,
    IPasswordHasher hasher,
    ITokenService tokenService,
    ILogger<LoginHandler> logger)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(
    LoginCommand request,
    CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Login attempt for {Username}", request.Username);
        // 1. Trouver le user
        var user = await _userRepo
        .GetByUsernameAsync(request.Username);
        if (user == null)
            return new AuthResponse(false, null, false,
            "Invalid credentials");
        // 2. Vérifier lockout
        if (user.LockedUntil.HasValue
        && user.LockedUntil > DateTime.UtcNow)
            return new AuthResponse(false, null, false,
            "Account locked. Try again later.");
        // 3. Vérifier password
        if (!_hasher.VerifyPassword(
        request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
                user.LockedUntil =
                DateTime.UtcNow.AddMinutes(15);
            await _userRepo.UpdateAsync(user);
            return new AuthResponse(false, null, false,
            "Invalid credentials");
        }
        // 4. Reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);
        // 5. MFA check
        if (user.MfaEnabled)
        {
            var mfaToken = _tokenService
            .GenerateMfaToken(user);
            return new AuthResponse(
            true, mfaToken, true,
            "MFA verification required");
        }
        // 6. Pas de MFA - JWT direct
        var token = _tokenService
        .GenerateAccessToken(user, user.Role);
        return new AuthResponse(
        true, token, false,
        "Login successful");
    }
}