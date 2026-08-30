using System.Reflection;
using System.Runtime.ExceptionServices;
using FluentAssertions;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Tdd;

/// <summary>
/// Reflection bridge used only during the RED phase for contracts whose production
/// types do not exist yet. It keeps the test project compilable so CI can report
/// behavioral failures rather than stopping at compiler errors. Once a vertical
/// reaches GREEN, its tests should be migrated to direct strongly typed calls.
/// </summary>
internal static class FutureContract
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
        var method = RequireMethod(type, methodName, isStatic: true, arguments.Length);
        return Invoke(method, null, type.FullName ?? type.Name, arguments)
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} returned null.");
    }

    public static object? InvokeInstance(object instance, string methodName, params object?[] arguments)
    {
        var type = instance.GetType();
        var method = RequireMethod(type, methodName, isStatic: false, arguments.Length);
        return Invoke(method, instance, type.FullName ?? type.Name, arguments);
    }

    public static T Read<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull($"'{instance.GetType().FullName}' must expose property '{propertyName}'");

        return (T)property!.GetValue(instance)!;
    }

    public static bool HasPublicSetter(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull($"'{instance.GetType().FullName}' must expose property '{propertyName}'");
        return property!.SetMethod?.IsPublic == true;
    }

    private static MethodInfo RequireMethod(Type type, string methodName, bool isStatic, int argumentCount)
    {
        var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
        var candidates = type
            .GetMethods(flags)
            .Where(method => method.Name == methodName)
            .Where(method => method.GetParameters().Length == argumentCount)
            .ToArray();

        candidates.Should().ContainSingle(
            $"'{type.FullName}.{methodName}' must expose one public {(isStatic ? "static" : "instance")} overload with {argumentCount} parameter(s)");
        return candidates.Single();
    }

    private static object? Invoke(
        MethodInfo method,
        object? target,
        string typeName,
        params object?[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw new InvalidOperationException($"Unable to invoke {typeName}.{method.Name}.");
        }
    }
}
