namespace RPG_ESI07.Domain.Entities;

public class UserConsent
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool AnalyticsConsent { get; set; } = false;
    public bool MarketingConsent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
}