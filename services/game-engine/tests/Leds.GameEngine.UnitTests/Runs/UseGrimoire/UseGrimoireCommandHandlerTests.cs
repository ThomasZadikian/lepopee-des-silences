using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.UseGrimoire;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.UseGrimoire;

public sealed class UseGrimoireCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrowWhenRunItemOrCharacterIsMissing()
    {
        var run = CreateRun(out var characterId, out var item);
        var repo = Repo(run);
        var catalog = new Mock<ICatalogContentGateway>();
        var handler = Handler(repo, catalog);

        await FluentActions.Awaiting(() => handler.Handle(
            new UseGrimoireCommand(Guid.NewGuid(), item.Id.Value, characterId), default))
            .Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => handler.Handle(
            new UseGrimoireCommand(run.Id.Value, Guid.NewGuid(), characterId), default))
            .Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => handler.Handle(
            new UseGrimoireCommand(run.Id.Value, item.Id.Value, Guid.NewGuid()), default))
            .Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_ShouldRejectMissingItemDefinitionGrantSkillAndSkillDefinition()
    {
        var run = CreateRun(out var characterId, out var item);
        var repo = Repo(run);
        var catalog = new Mock<ICatalogContentGateway>();
        var handler = Handler(repo, catalog);

        catalog.Setup(g => g.GetItemDefinitionByKeyAsync(item.DefinitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Failure(Error.Create("missing", "missing")));
        await FluentActions.Awaiting(() => handler.Handle(
            new UseGrimoireCommand(run.Id.Value, item.Id.Value, characterId), default))
            .Should().ThrowAsync<DomainException>();

        catalog.Setup(g => g.GetItemDefinitionByKeyAsync(item.DefinitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(ItemDefinition([])));
        await FluentActions.Awaiting(() => handler.Handle(
            new UseGrimoireCommand(run.Id.Value, item.Id.Value, characterId), default))
            .Should().ThrowAsync<DomainException>();

        catalog.Setup(g => g.GetItemDefinitionByKeyAsync(item.DefinitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(ItemDefinition([
                new CatalogItemEquipmentEffect("GrantSkill", null, null, "skill.temp.test", null)
            ])));
        catalog.Setup(g => g.GetSkillDefinitionByKeyAsync("skill.temp.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CatalogSkillDefinition?)null);
        await FluentActions.Awaiting(() => handler.Handle(
            new UseGrimoireCommand(run.Id.Value, item.Id.Value, characterId), default))
            .Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_ShouldGrantAuthoredTemporarySkillConsumeItemAndPersist()
    {
        var run = CreateRun(out var characterId, out var item);
        var repo = Repo(run);
        var catalog = new Mock<ICatalogContentGateway>();
        catalog.Setup(g => g.GetItemDefinitionByKeyAsync(item.DefinitionKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(ItemDefinition([
                new CatalogItemEquipmentEffect("GrantSkill", null, null, "skill.temp.test", null)
            ])));
        catalog.Setup(g => g.GetSkillDefinitionByKeyAsync("skill.temp.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogSkillDefinition(
                "skill.temp.test", "Sort temporaire", "Test", "Damage", "SingleEnemy", "Damage",
                2, 0, 14, [], "Neutral", Category: "Magic", TacticalRange: 3,
                TacticalAreaShape: "Single", RequiresLineOfSight: true, Cooldown: 1));
        var handler = Handler(repo, catalog);

        var response = await handler.Handle(
            new UseGrimoireCommand(run.Id.Value, item.Id.Value, characterId), default);

        response.GrantedSkillKey.Should().Be("skill.temp.test");
        response.ItemDepleted.Should().BeTrue();
        run.PlayerSnapshot!.Characters.Single().Skills.Should().ContainSingle(s => s.SkillDefinitionKey == "skill.temp.test");
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UseGrimoireCommandHandler Handler(
        Mock<IRunRepository> repo,
        Mock<ICatalogContentGateway> catalog) =>
        new(repo.Object, catalog.Object, new Mock<IPlayerProfileGateway>().Object);

    private static Mock<IRunRepository> Repo(Run run)
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);
        return repo;
    }

    private static Run CreateRun(out Guid characterId, out RunItem item)
    {
        var run = TestGameEngineFactory.CreateRun();
        characterId = Guid.NewGuid();
        var stats = RunCharacterStatSnapshot.Create(100, 10, 8, 0, 10, 10, 5, 20, 0);
        var baseSkill = RunCharacterSkillSnapshot.Create(
            "skill.base", "Base", "Damage", "SingleEnemy", "Damage", 0, 0, 10,
            emotionalRegister: "Neutral");
        var character = RunCharacterSnapshot.Create(
            characterId, "character.player.self", "Porteur", stats, [baseSkill],
            emotionalRegisterCode: "Neutral");
        run.AttachPlayerSnapshot(RunPlayerSnapshot.Create(
            run.PlayerId, "Joueur", [character], DateTimeOffset.UtcNow));
        item = RunItem.Create(
            "item.grimoire.test", "Grimoire", "Test", RunItemType.Grimoire, default, 1,
            RunItemEffectType.GrantTemporarySkill, 0);
        run.AddRunItem(item);
        return run;
    }

    private static CatalogItemDefinitionSnapshot ItemDefinition(
        IReadOnlyCollection<CatalogItemEquipmentEffect> effects) =>
        new("item.grimoire.test", "1", "Grimoire", "Test", null, "Grimoire", "", "Common",
            "Use", "Run", "Stack", 1, false, true, EquipmentEffects: effects);
}
