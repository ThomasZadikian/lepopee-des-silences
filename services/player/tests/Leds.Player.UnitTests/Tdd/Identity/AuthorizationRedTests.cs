using FluentAssertions;
using Leds.Player.UnitTests.Tdd;

namespace Leds.Player.UnitTests.Tdd.Identity;

public sealed class AuthorizationRedTests
{
    [Fact]
    public void Player_ShouldHaveGameplayRightsOnly()
    {
        var role = GetRole("Player");
        var permissions = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.RolePermissions");

        FutureContract.InvokeStatic(permissions, "CanUseDeveloperTools", role).Should().Be(false);
        FutureContract.InvokeStatic(permissions, "CanAdministerAccounts", role).Should().Be(false);
    }

    [Fact]
    public void Developer_ShouldHaveDeveloperToolsButNotAccountAdministration()
    {
        var role = GetRole("Developer");
        var permissions = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.RolePermissions");

        FutureContract.InvokeStatic(permissions, "CanUseDeveloperTools", role).Should().Be(true);
        FutureContract.InvokeStatic(permissions, "CanAdministerAccounts", role).Should().Be(false);
    }

    [Fact]
    public void Administrator_ShouldHaveAccountAdministrationAndDeveloperTools()
    {
        var role = GetRole("Administrator");
        var permissions = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.RolePermissions");

        FutureContract.InvokeStatic(permissions, "CanUseDeveloperTools", role).Should().Be(true);
        FutureContract.InvokeStatic(permissions, "CanAdministerAccounts", role).Should().Be(true);
    }

    private static object GetRole(string roleName)
    {
        var roleType = FutureContract.RequireDomainType("Leds.Player.Domain.Identity.AccountRole");
        roleType.IsEnum.Should().BeTrue("AccountRole should be a closed role set in the initial model");
        return Enum.Parse(roleType, roleName, ignoreCase: false);
    }
}
