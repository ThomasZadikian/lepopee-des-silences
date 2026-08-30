namespace Leds.Player.Domain.Identity;

public enum AccountRole
{
    Player = 0,
    Developer = 1,
    Administrator = 2
}

public static class RolePermissions
{
    public static bool CanUseDeveloperTools(AccountRole role) =>
        role is AccountRole.Developer or AccountRole.Administrator;

    public static bool CanAdministerAccounts(AccountRole role) =>
        role is AccountRole.Administrator;
}
