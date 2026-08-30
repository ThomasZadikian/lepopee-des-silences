using System.Reflection;
using System.Runtime.ExceptionServices;
using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Tdd.Identity;

/// <summary>
/// First RED slice for SFD-010 v1.1: account registration foundation.
/// These tests intentionally describe the target contract before the production
/// implementation exists. Keep this suite deterministic: no network, database,
/// wall-clock access or random values are allowed here.
/// </summary>
public sealed class AccountRegistrationRedTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewAccount_ShouldStartWithoutCharacter_UntilArchetypeSelection()
    {
        var account = PlayerProfile.Create("Nocturne", Now);

        account.Roster.Characters.Should().BeEmpty(
            "character creation belongs to the archetype-selection onboarding step");
    }

    [Fact]
    public void EmailAddress_ShouldNormalizeTrimAndCase()
    {
        var emailType = FutureIdentityContract.RequireDomainType("Leds.Player.Domain.Identity.EmailAddress");
        var email = FutureIdentityContract.InvokeStatic(emailType, "Create", "  Player@Example.COM  ");

        FutureIdentityContract.Read<string>(email, "Value")
            .Should().Be("player@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    [InlineData("@missing-local.example")]
    public void EmailAddress_ShouldRejectInvalidValues(string value)
    {
        var emailType = FutureIdentityContract.RequireDomainType("Leds.Player.Domain.Identity.EmailAddress");

        var act = () => FutureIdentityContract.InvokeStatic(emailType, "Create", value);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void PasswordPolicy_ShouldRejectPasswordsShorterThanTwelveCharacters()
    {
        var policyType = FutureIdentityContract.RequireDomainType("Leds.Player.Domain.Identity.PasswordPolicy");

        var act = () => FutureIdentityContract.InvokeStatic(policyType, "EnsureAcceptable", "elevenchars");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void PasswordPolicy_ShouldNotRequireArtificialCharacterClasses()
    {
        var policyType = FutureIdentityContract.RequireDomainType("Leds.Player.Domain.Identity.PasswordPolicy");

        var act = () => FutureIdentityContract.InvokeStatic(policyType, "EnsureAcceptable", "correcthorse");

        act.Should().NotThrow(
            "the approved policy requires 12+ characters but no forced uppercase/digit/symbol composition");
    }

    [Fact]
    public void RegisteredIdentity_ShouldStartWithEmailUnverifiedAndMfaNotConfigured()
    {
        var identity = CreateRegisteredIdentity();

        FutureIdentityContract.Read<bool>(identity, "IsEmailVerified").Should().BeFalse();
        FutureIdentityContract.Read<bool>(identity, "IsMfaConfigured").Should().BeFalse();
    }

    [Fact]
    public void RegisteredIdentity_ShouldDefaultToPlayerRole()
    {
        var identity = CreateRegisteredIdentity();

        FutureIdentityContract.Read<object>(identity, "Role")
            .ToString().Should().Be("Player");
    }

    private static object CreateRegisteredIdentity()
    {
        var emailType = FutureIdentityContract.RequireDomainType("Leds.Player.Domain.Identity.EmailAddress");
        var email = FutureIdentityContract.InvokeStatic(emailType, "Create", "player@example.com");
        var identityType = FutureIdentityContract.RequireDomainType("Leds.Player.Domain.Identity.UserIdentity");

        return FutureIdentityContract.InvokeStatic(
            identityType,
            "Register",
            email,
            "argon2id$unit-test-hash",
            Now);
    }
}

internal static class FutureIdentityContract
{
    private static readonly Assembly DomainAssembly = typeof(PlayerProfile).Assembly;

    public static Type RequireDomainType(string fullName)
    {
        var type = DomainAssembly.GetType(fullName, throwOnError: false, ignoreCase: false);
        type.Should().NotBeNull($"the RED contract requires production type '{fullName}'");
        return type!;
    }

    public static object InvokeStatic(Type type, string methodName, params object?[] arguments)
    {
        var candidates = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == methodName)
            .Where(method => method.GetParameters().Length == arguments.Length)
            .ToArray();

        candidates.Should().ContainSingle(
            $"'{type.FullName}.{methodName}' must expose one public static overload with {arguments.Length} parameter(s)");

        try
        {
            return candidates.Single().Invoke(null, arguments)
                ?? throw new InvalidOperationException($"{type.FullName}.{methodName} returned null.");
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    public static T Read<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull($"'{instance.GetType().FullName}' must expose property '{propertyName}'");

        var value = property!.GetValue(instance);
        value.Should().BeAssignableTo<T>();
        return (T)value!;
    }
}
