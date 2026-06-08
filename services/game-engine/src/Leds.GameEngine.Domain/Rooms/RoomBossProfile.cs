using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Rooms;

public sealed record RoomBossProfile(
    string BossId,
    string Name,
    RoomType RoomType,
    string DangerHint,
    string EnemyTemplateKey)
{
    public static RoomBossProfile Create(
        string bossId,
        string name,
        RoomType roomType,
        string dangerHint,
        string enemyTemplateKey)
    {
        if (string.IsNullOrWhiteSpace(bossId))
        {
            throw new DomainException("Boss id is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Boss name is required.");
        }

        if (string.IsNullOrWhiteSpace(dangerHint))
        {
            throw new DomainException("Boss danger hint is required.");
        }

        if (string.IsNullOrWhiteSpace(enemyTemplateKey))
        {
            throw new DomainException("Boss enemy template key is required.");
        }

        return new RoomBossProfile(
            bossId.Trim(),
            name.Trim(),
            roomType,
            dangerHint.Trim(),
            enemyTemplateKey.Trim());
    }
}