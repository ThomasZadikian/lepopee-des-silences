using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Application.Players.Equipment;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;
using Moq;

namespace Leds.Player.UnitTests.Application.Players;

public sealed class EquipmentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Preview_ShouldReturnPlanAndRejectMissingPlayer()
    {
        var fixture = Fixture();
        var handler = new PreviewEquipItemQueryHandler(fixture.Repository.Object, fixture.Planner);
        var query = new PreviewEquipItemQuery(
            fixture.Profile.Id.Value, fixture.Character.Id.Value, fixture.Item.Id.Value,
            EquipmentPosition.Chest);

        var plan = await handler.Handle(query, CancellationToken.None);

        plan.CanEquip.Should().BeTrue();
        await new PreviewEquipItemQueryHandler(MissingRepository().Object, fixture.Planner)
            .Invoking(candidate => candidate.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Equip_ShouldPersistAllowedPlanAndRejectBlockedPlan()
    {
        var fixture = Fixture();
        var command = new EquipItemInstanceCommand(
            fixture.Profile.Id.Value, fixture.Character.Id.Value, fixture.Item.Id.Value,
            EquipmentPosition.Chest);
        var handler = new EquipItemInstanceCommandHandler(
            fixture.Repository.Object, fixture.Planner, new FixedTimeProvider(Now));

        var result = await handler.Handle(command, CancellationToken.None);

        result.Characters.Single().Items.Single().Position.Should().Be(EquipmentPosition.Chest.ToString());
        fixture.Repository.Verify(repository => repository.SaveAsync(
            fixture.Profile, It.IsAny<CancellationToken>()), Times.Once);

        var blocked = Fixture(["Ring"]);
        var blockedHandler = new EquipItemInstanceCommandHandler(
            blocked.Repository.Object, blocked.Planner, new FixedTimeProvider(Now));
        await blockedHandler.Invoking(candidate => candidate.Handle(new EquipItemInstanceCommand(
                blocked.Profile.Id.Value, blocked.Character.Id.Value, blocked.Item.Id.Value,
                EquipmentPosition.Chest), CancellationToken.None))
            .Should().ThrowAsync<DomainException>().WithMessage("*SlotNotAllowed*");
    }

    [Fact]
    public async Task Equip_ShouldRejectMissingPlayer()
    {
        var fixture = Fixture();
        var handler = new EquipItemInstanceCommandHandler(
            MissingRepository().Object, fixture.Planner, new FixedTimeProvider(Now));

        await handler.Invoking(candidate => candidate.Handle(new EquipItemInstanceCommand(
                Guid.NewGuid(), fixture.Character.Id.Value, fixture.Item.Id.Value,
                EquipmentPosition.Chest), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Unequip_ShouldDetachItemAndRejectMissingPlayer()
    {
        var fixture = Fixture();
        fixture.Profile.EquipItem(
            fixture.Character.Id, fixture.Item.Id, EquipmentPosition.Chest,
            [EquipmentSlotKind.Chest], Now.AddMinutes(-1));
        var command = new UnequipItemInstanceCommand(
            fixture.Profile.Id.Value, fixture.Character.Id.Value, fixture.Item.Id.Value);
        var handler = new UnequipItemInstanceCommandHandler(
            fixture.Repository.Object, new FixedTimeProvider(Now));

        var result = await handler.Handle(command, CancellationToken.None);

        result.Characters.Single().Items.Should().BeEmpty();
        await new UnequipItemInstanceCommandHandler(MissingRepository().Object, new FixedTimeProvider(Now))
            .Invoking(candidate => candidate.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    private static TestFixture Fixture(IReadOnlyCollection<string>? allowedSlots = null)
    {
        var profile = PlayerProfile.Create("Player", Now.AddHours(-1));
        var archetype = new ArchetypeDefinitionSnapshot(
            "archetype.test", PlayerCharacterStatBlock.CreateDefaultPorteur(), [], [],
            ["skill.test"], ["skill.test"]);
        var character = profile.CreatePlayableCharacter("Hero", archetype, Now.AddHours(-1));
        profile.AddPermanentItems(["item.armour"], Guid.NewGuid(), Now.AddMinutes(-2));
        var item = profile.PermanentItems.Single();
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(candidate => candidate.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        repository.Setup(candidate => candidate.SaveAsync(profile, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var equipment = new EquipmentGateway(new EquipmentDefinitionSnapshot(
            "item.armour", "Armour", allowedSlots ?? ["Chest"], null, [], []));
        return new(profile, character, item, repository,
            new EquipmentChangePlanner(equipment, new ArchetypeGateway(archetype)));
    }

    private static Mock<IPlayerProfileRepository> MissingRepository()
    {
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(candidate => candidate.GetByIdAsync(
                It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);
        return repository;
    }

    private sealed record TestFixture(
        PlayerProfile Profile, PlayerCharacter Character, PlayerPermanentItem Item,
        Mock<IPlayerProfileRepository> Repository, EquipmentChangePlanner Planner);

    private sealed class EquipmentGateway(EquipmentDefinitionSnapshot definition) : IEquipmentDefinitionGateway
    {
        public Task<EquipmentDefinitionSnapshot?> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<EquipmentDefinitionSnapshot?>(
                string.Equals(key, definition.Key, StringComparison.OrdinalIgnoreCase) ? definition : null);
    }

    private sealed class ArchetypeGateway(ArchetypeDefinitionSnapshot definition) : IArchetypeDefinitionGateway
    {
        public Task<ArchetypeDefinitionSnapshot?> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<ArchetypeDefinitionSnapshot?>(definition);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
