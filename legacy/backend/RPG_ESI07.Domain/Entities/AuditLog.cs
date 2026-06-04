namespace RPG_ESI07.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; } // Nullable si event système
    public string EventType { get; set; } = string.Empty; // LOGIN_SUCCESS, MFA_FAILED, etc.
    public string? EventData { get; set; } // JSON détails
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
}