using FluentAssertions;
using Leds.GameEngine.Api.Controllers;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Leds.GameEngine.UnitTests.Players;

public sealed class EquipmentInstanceCoverageTests
{
    [Fact]
    public async Task PreviewHandler_ShouldValidateAndForwardRequest()
    {
        var gateway = new Mock<IPlayerProfileGateway>();
        gateway.Setup(candidate => candidate.PreviewEquipmentChangeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), "Chest",
                It.IsAny<EquipmentResourceContextView?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Plan());
        var handler = new PreviewEquipmentChangeQueryHandler(gateway.Object);
        var request = new PreviewEquipmentChangeQuery(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Chest", new(80, 20));

        var result = await handler.Handle(request, CancellationToken.None);

        result.CanEquip.Should().BeTrue();
        gateway.VerifyAll();
    }

    [Fact]
    public async Task PreviewHandler_ShouldRejectMissingIdentityAndPosition()
    {
        var handler = new PreviewEquipmentChangeQueryHandler(Mock.Of<IPlayerProfileGateway>());

        await handler.Invoking(candidate => candidate.Handle(new PreviewEquipmentChangeQuery(
                Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Chest"), CancellationToken.None))
            .Should().ThrowAsync<DomainException>();
        await handler.Invoking(candidate => candidate.Handle(new PreviewEquipmentChangeQuery(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " "), CancellationToken.None))
            .Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task EquipAndUnequipHandlers_ShouldForwardWhenNoCombatIsActive()
    {
        var profile = Profile();
        var gateway = new Mock<IPlayerProfileGateway>();
        gateway.Setup(candidate => candidate.EquipItemInstanceAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), "Chest",
                It.IsAny<EquipmentResourceContextView?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        gateway.Setup(candidate => candidate.UnequipItemInstanceAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var runs = new Mock<IRunRepository>();
        runs.Setup(candidate => candidate.GetOpenByPlayerIdAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leds.GameEngine.Domain.Runs.Run?)null);
        var itemId = Guid.NewGuid();

        var equipped = await new EquipItemInstanceCommandHandler(gateway.Object, runs.Object).Handle(
            new EquipItemInstanceCommand(Guid.NewGuid(), Guid.NewGuid(), itemId, "Chest"),
            CancellationToken.None);
        var unequipped = await new UnequipItemInstanceCommandHandler(gateway.Object, runs.Object).Handle(
            new UnequipItemInstanceCommand(Guid.NewGuid(), Guid.NewGuid(), itemId),
            CancellationToken.None);

        equipped.Should().BeSameAs(profile);
        unequipped.Should().BeSameAs(profile);
    }

    [Fact]
    public async Task UnequipHandler_ShouldRejectMissingItemIdentity()
    {
        var handler = new UnequipItemInstanceCommandHandler(
            Mock.Of<IPlayerProfileGateway>(), Mock.Of<IRunRepository>());

        await handler.Invoking(candidate => candidate.Handle(
                new UnequipItemInstanceCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty),
                CancellationToken.None))
            .Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Controller_ShouldForwardEveryInstanceEquipmentRequest()
    {
        var sender = new Mock<ISender>();
        sender.Setup(candidate => candidate.Send(
                It.IsAny<PreviewEquipmentChangeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Plan());
        sender.Setup(candidate => candidate.Send(
                It.IsAny<EquipItemInstanceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile());
        sender.Setup(candidate => candidate.Send(
                It.IsAny<UnequipItemInstanceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Profile());
        var controller = new PlayerProgressionController(sender.Object);

        var preview = await controller.PreviewEquipmentChange(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Chest", new(90, 10), CancellationToken.None);
        var equip = await controller.EquipItemInstance(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Chest", null, CancellationToken.None);
        var unequip = await controller.UnequipItemInstance(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        preview.Result.Should().BeOfType<OkObjectResult>();
        equip.Result.Should().BeOfType<OkObjectResult>();
        unequip.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task EquipmentAwareGateway_ShouldForwardAndEnrichInstanceOperations()
    {
        var profile = Profile();
        var inner = new Mock<IPlayerProfileGateway>();
        inner.Setup(candidate => candidate.PreviewEquipmentChangeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<EquipmentResourceContextView?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Plan());
        inner.Setup(candidate => candidate.EquipItemInstanceAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<EquipmentResourceContextView?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        inner.Setup(candidate => candidate.UnequipItemInstanceAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var gateway = new EquipmentAwarePlayerProfileGateway(
            inner.Object,
            new PlayerSkillMerger(Mock.Of<ICatalogContentGateway>()),
            new PlayerStatMerger());

        (await gateway.PreviewEquipmentChangeAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Chest", null, CancellationToken.None))
            .Should().NotBeNull();
        (await gateway.EquipItemInstanceAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Chest", null, CancellationToken.None))
            .Should().BeSameAs(profile);
        (await gateway.UnequipItemInstanceAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None))
            .Should().BeSameAs(profile);
    }

    private static PlayerProfileView Profile() =>
        new(Guid.NewGuid(), "Player", [], new PlayerProgressionView());

    private static EquipmentChangePlanView Plan()
    {
        var stats = new EquipmentStatsView(100, 10, 0, 5, 0, 0, 10, 0, 0, 20, 0, 4);
        return new EquipmentChangePlanView(
            "Chest", new(Guid.NewGuid(), "item.armour", "Armour"), null, true, [],
            stats, stats, [], [], [], [], [], 100, 100, 20, 20, ["Chest"], []);
    }
}
