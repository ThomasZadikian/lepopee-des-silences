using System.ComponentModel.DataAnnotations;

namespace RPG_ESI07.Application.Configuration; 
public class JwtSettings
{
    [Required]
    [MinLength(32, ErrorMessage = "JWT Secret doit faire au minimum 32 caractères.")]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public int ExpirationMinutes { get; set; } = 15;
    public int MfaTokenExpirationMinutes { get; set; } = 5;
}