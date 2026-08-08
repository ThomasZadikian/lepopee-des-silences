using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence.Mappers;
using Leds.GameEngine.UnitTests.Common;

namespace Leds.GameEngine.UnitTests.Combats.Tactical;

/// <summary>
/// Vérifie spécifiquement que <c>TacticalOriginX</c>/<c>TacticalOriginY</c> (Chantier 4 — arène
/// de combat locale) survivent l'aller-retour de persistance, sans quoi une arène recadrée près
/// d'un bord de salle se rechargerait comme si elle couvrait la salle entière depuis (0,0).
/// </summary>
public sealed class TacticalCombatPersistenceMapperOriginTests
{
    [Fact]
    public void ToEntity_ThenToDomain_ShouldPreserveANonZeroBattlefieldOrigin()
    {
        var grid = RoomGrid.CreateInitial(
            width: 10, height: 10, movementBudget: 100,
            startX: 0, startY: 0, nodes: []);

        var battlefield = TacticalBattlefield.FromRoomGridRegion(
            grid, originX: 3, originY: 5, width: 4, height: 4);

        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        var combat = TacticalCombat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            battlefield,
            [(ally, new GridPosition(0, 0))],
            [(enemy, new GridPosition(1, 1))],
            DateTime.UtcNow,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());

        var entity = TacticalCombatPersistenceMapper.ToEntity(combat, Guid.NewGuid());

        entity.TacticalOriginX.Should().Be(3);
        entity.TacticalOriginY.Should().Be(5);
        entity.TacticalWidth.Should().Be(4);
        entity.TacticalHeight.Should().Be(4);

        var rehydrated = TacticalCombatPersistenceMapper.ToDomain(entity, TestEmotionalAffinityMatrix.Create());

        rehydrated.Battlefield.OriginX.Should().Be(3);
        rehydrated.Battlefield.OriginY.Should().Be(5);
        rehydrated.Battlefield.Width.Should().Be(4);
        rehydrated.Battlefield.Height.Should().Be(4);
    }

    [Fact]
    public void ToDomain_ShouldDefaultOriginToZeroZero_ForACombatPersistedBeforeTheColumnsExisted()
    {
        var grid = RoomGrid.CreateInitial(
            width: 6, height: 6, movementBudget: 50,
            startX: 0, startY: 0, nodes: []);

        var battlefield = TacticalBattlefield.FromRoomGrid(grid);

        var ally = Combatant.CreateAlly("player.self", "Hero", "Fighter", 100);
        var enemy = Combatant.CreateEnemy("enemy.sentinel", "Sentinel", "Guard", 80);

        var combat = TacticalCombat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            battlefield,
            [(ally, new GridPosition(0, 0))],
            [(enemy, new GridPosition(1, 1))],
            DateTime.UtcNow,
            emotionalAffinityMatrix: TestEmotionalAffinityMatrix.Create());

        var entity = TacticalCombatPersistenceMapper.ToEntity(combat, Guid.NewGuid());
        // Simule une ligne écrite avant l'ajout des colonnes d'origine : la valeur par défaut de
        // la colonne (0) est ce qu'un vieux combat lirait.
        entity.TacticalOriginX = 0;
        entity.TacticalOriginY = 0;

        var rehydrated = TacticalCombatPersistenceMapper.ToDomain(entity, TestEmotionalAffinityMatrix.Create());

        rehydrated.Battlefield.OriginX.Should().Be(0);
        rehydrated.Battlefield.OriginY.Should().Be(0);
    }
}
