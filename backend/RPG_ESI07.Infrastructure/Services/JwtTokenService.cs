using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RPG_ESI07.Application.Configuration;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RPG_ESI07.Infrastructure.Services;

public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateAccessToken(User user, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("username", user.Username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateMfaToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("mfa_pending", "true")
        };
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(
        key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _settings.MfaTokenExpirationMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public int? ValidateTokenAndGetUserId(string token)
    {
        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                IssuerSigningKey = key
            }, out _);

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return sub != null ? int.Parse(sub) : null;
        }
        catch
        {
            return null;
        }
    }
}