namespace RPG_ESI07.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public byte[] Email { get; set; } = Array.Empty<byte>(); // Chiffré AES-256
    public string PasswordHash { get; set; } = string.Empty; // Argon2id
    public byte[]? MfaSecret { get; set; } // Chiffré, nullable
    public bool MfaEnabled { get; set; } = false;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIP { get; set; }
    public DateTime? DeletedAt { get; set; } // Soft delete RGPD
    public string? DeletionReason { get; set; }
    public string Role { get; set; } = Constants.RolePlayer;

    // Navigation properties
    public PlayerProfile? PlayerProfile { get; set; }

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}