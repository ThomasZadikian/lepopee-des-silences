using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record RoomBossProfileDto(
    string BossId,
    string Name,
    string RoomType,
    string DangerHint)
{
    public static RoomBossProfileDto FromDomain(RoomBossProfile bossProfile)
    {
        return new RoomBossProfileDto(
            bossProfile.BossId,
            bossProfile.Name,
            bossProfile.RoomType.ToString(),
            bossProfile.DangerHint);
    }
}