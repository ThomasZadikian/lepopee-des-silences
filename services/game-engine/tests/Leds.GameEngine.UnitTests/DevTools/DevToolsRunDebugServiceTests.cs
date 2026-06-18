using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.DevTools;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.DevTools;

public sealed class DevToolsRunDebugServiceTests
{
    [Fact]
    public async Task KillAllCurrentCombatEnemies_ShouldDefeatEnemiesOnly()
    {
        var setup = CreateRunWithActiveCombat(enemyCount: 2);
        var service = await CreateServiceAsync(setup.Run);

        var result = await service.KillAllCurrentCombatEnemiesAsync(setup.Run.Id.Value);

        result.Combat.Enemies.Should().OnlyContain(enemy => enemy.Status == "Defeated" && enemy.CurrentVitality == 0);
        result.Combat.Allies.Should().OnlyContain(ally => ally.Status == "Active" && ally.CurrentVitality > 0);
        result.Combat.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task KillCurrentCombatEnemy_ShouldDefeatOnlyTargetEnemy()
    {
        var setup = CreateRunWithActiveCombat(enemyCount: 2);
        var service = await CreateServiceAsync(setup.Run);
        var targetEnemy = setup.Combat.Enemies.First();

        var result = await service.KillCurrentCombatEnemyAsync(setup.Run.Id.Value, targetEnemy.Id.Value);

        result.Combat.Enemies.Single(enemy => enemy.Id == targetEnemy.Id.Value).Status.Should().Be("Defeated");
        result.Combat.Enemies.Single(enemy => enemy.Id != targetEnemy.Id.Value).Status.Should().Be("Active");
        result.Combat.Status.Should().Be("Active");
    }

    [Fact]
    public async Task SetCurrentCombatantVitals_ShouldUpdateVitalityAndGuard()
    {
        var setup = CreateRunWithActiveCombat(enemyCount: 1);
        var service = await CreateServiceAsync(setup.Run);
        var ally = setup.Combat.Allies.Single();

        var result = await service.SetCurrentCombatantVitalsAsync(
            setup.Run.Id.Value,
            ally.Id.Value,
            vitality: 1,
            guard: 99);

        var updatedAlly = result.Combat.Allies.Single();
        updatedAlly.CurrentVitality.Should().Be(1);
        updatedAlly.Guard.Should().Be(99);
        updatedAlly.Status.Should().Be("Active");
    }

    [Fact]
    public async Task SetCurrentCombatantVitals_ShouldRejectNegativeValues()
    {
        var setup = CreateRunWithActiveCombat(enemyCount: 1);
        var service = await CreateServiceAsync(setup.Run);
        var ally = setup.Combat.Allies.Single();

        var act = () => service.SetCurrentCombatantVitalsAsync(
            setup.Run.Id.Value,
            ally.Id.Value,
            vitality: 1,
            guard: -1);

        await act.Should().ThrowAsync<DomainException>();
    }

    private static async Task<DevToolsRunDebugService> CreateServiceAsync(Run run)
    {
        var runRepository = new InMemoryRunRepository();
        await runRepository.AddAsync(run, CancellationToken.None);

        return new DevToolsRunDebugService(
            runRepository,
            Mock.Of<IRunGenerator>(),
            Mock.Of<ICatalogContentGateway>(),
            Mock.Of<ICatalogCurseDefinitionProvider>());
    }

    private static (Run Run, Combat Combat) CreateRunWithActiveCombat(int enemyCount)
    {
        var runWithNode = TestGameEngineFactory.CreateRunWithSelectedTargetNode(NodeEventType.Combat);
        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100, 0, []);
        var enemies = Enumerable.Range(0, enemyCount)
            .Select(index => Combatant.CreateEnemy($"enemy.{index}", $"Enemy {index}", "Guard", 80, []))
            .ToArray();

        var combat = Combat.Create(
            CombatId.New(),
            runWithNode.Run.Id,
            RoomId.New(),
            NodeId.New(),
            [ally],
            enemies);

        runWithNode.Run.StartCombat(combat);
        return (runWithNode.Run, combat);
    }
}
