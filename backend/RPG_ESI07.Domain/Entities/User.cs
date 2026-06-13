using System.Text.Json.Serialization;

namespace RPG_ESI07.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    [JsonIgnore]
    public byte[] Email { get; set; } = Array.Empty<byte>();

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonIgnore]
    public byte[]? MfaSecret { get; set; }

    public bool MfaEnabled { get; set; } = false;

    [JsonIgnore]
    public int FailedLoginAttempts { get; set; } = 0;

    [JsonIgnore]
    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    [JsonIgnore]
    public string? LastLoginIP { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletionReason { get; set; }
    public string Role { get; set; } = Constants.RolePlayer;

    public PlayerProfile? PlayerProfile { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}