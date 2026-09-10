using FluentAssertions;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class DeveloperSandboxRunTests
{
    [Fact]
    public void StartNew_ShouldExposeDeveloperSandboxMode()
    {
        var sandbox = TestGameEngineFactory.CreateRun(mode: RunMode.DeveloperSandbox);

        sandbox.Mode.Should().Be(RunMode.DeveloperSandbox);
        RunDto.FromDomain(sandbox).Mode.Should().Be("DeveloperSandbox");
    }
}
