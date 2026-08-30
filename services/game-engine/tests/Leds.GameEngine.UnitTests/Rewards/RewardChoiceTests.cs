using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rewards;

namespace Leds.GameEngine.UnitTests.Rewards;

public sealed class RewardChoiceTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var choice = RewardChoice.Create(
            RewardType.Heal,
            "Soin",
            "Restaure 15 PV.",
            "heal:15");

        choice.Id.Value.Should().NotBeEmpty();
        choice.RewardType.Should().Be(RewardType.Heal);
        choice.Label.Should().Be("Soin");
        choice.Description.Should().Be("Restaure 15 PV.");
        choice.PayloadKey.Should().Be("heal:15");
    }

    [Fact]
    public void Create_ShouldTrimLabelDescriptionAndPayloadKey()
    {
        var choice = RewardChoice.Create(
            RewardType.Heal,
            "  Soin  ",
            "  Restaure 15 PV.  ",
            "  heal:15  ");

        choice.Label.Should().Be("Soin");
        choice.Description.Should().Be("Restaure 15 PV.");
        choice.PayloadKey.Should().Be("heal:15");
    }

    [Fact]
    public void Create_ShouldThrow_WhenLabelIsNullOrWhitespace()
    {
        var act1 = () => RewardChoice.Create(RewardType.Heal, "", "Desc", "payload");
        var act2 = () => RewardChoice.Create(RewardType.Heal, "   ", "Desc", "payload");

        act1.Should().Throw<DomainException>().WithMessage("Reward choice label is required.");
        act2.Should().Throw<DomainException>().WithMessage("Reward choice label is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDescriptionIsNullOrWhitespace()
    {
        var act1 = () => RewardChoice.Create(RewardType.Heal, "Label", "", "payload");
        var act2 = () => RewardChoice.Create(RewardType.Heal, "Label", "   ", "payload");

        act1.Should().Throw<DomainException>().WithMessage("Reward choice description is required.");
        act2.Should().Throw<DomainException>().WithMessage("Reward choice description is required.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenPayloadKeyIsNullOrWhitespace()
    {
        var act1 = () => RewardChoice.Create(RewardType.Heal, "Label", "Desc", "");
        var act2 = () => RewardChoice.Create(RewardType.Heal, "Label", "Desc", "   ");

        act1.Should().Throw<DomainException>().WithMessage("Reward choice payload key is required.");
        act2.Should().Throw<DomainException>().WithMessage("Reward choice payload key is required.");
    }

    [Fact]
    public void Rehydrate_ShouldRestoreChoice_WithGivenId()
    {
        var id = RewardChoiceId.New();

        var choice = RewardChoice.Rehydrate(
            id,
            RewardType.Heal,
            "Soin",
            "Restaure 15 PV.",
            "heal:15");

        choice.Id.Should().Be(id);
        choice.RewardType.Should().Be(RewardType.Heal);
        choice.Label.Should().Be("Soin");
        choice.Description.Should().Be("Restaure 15 PV.");
        choice.PayloadKey.Should().Be("heal:15");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var choice1 = RewardChoice.Create(RewardType.Heal, "Label1", "Desc1", "payload1");
        var choice2 = RewardChoice.Create(RewardType.Heal, "Label2", "Desc2", "payload2");

        choice1.Id.Should().NotBe(choice2.Id);
    }

    [Fact]
    public void Create_ShouldDefaultSourceEnemyFields_ToNull()
    {
        var choice = RewardChoice.Create(RewardType.Heal, "Soin", "Restaure 15 PV.", "heal:15");

        choice.SourceEnemyKey.Should().BeNull();
        choice.SourceEnemyDisplayName.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCarrySourceEnemyFields_WhenProvided()
    {
        var choice = RewardChoice.Create(
            RewardType.TemporaryItem,
            "Peau de serpent",
            "Une mue encore souple.",
            "item:item.consumable.peau-de-serpent:...",
            sourceEnemyKey: "enemy.forest.chimere-serpentaire",
            sourceEnemyDisplayName: "Chimere Serpentaire");

        choice.SourceEnemyKey.Should().Be("enemy.forest.chimere-serpentaire");
        choice.SourceEnemyDisplayName.Should().Be("Chimere Serpentaire");
    }

    [Fact]
    public void Create_ShouldDefaultCurrencyCosts_ToZero()
    {
        var choice = RewardChoice.Create(RewardType.Heal, "Soin", "Restaure 15 PV.", "heal:15");

        choice.PalaceShardCost.Should().Be(0);
        choice.HimLitShardCost.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldCarryCurrencyCosts_WhenProvided()
    {
        var choice = RewardChoice.Create(
            RewardType.TemporaryItem,
            "Objet du marchand",
            "Un objet à vendre.",
            "item:item.consumable.marchand:...",
            palaceShardCost: 500,
            himLitShardCost: 25);

        choice.PalaceShardCost.Should().Be(500);
        choice.HimLitShardCost.Should().Be(25);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Create_ShouldThrow_WhenEitherCostIsNegative(int palaceShardCost, int himLitShardCost)
    {
        var act = () => RewardChoice.Create(
            RewardType.TemporaryItem, "Label", "Desc", "payload",
            palaceShardCost: palaceShardCost, himLitShardCost: himLitShardCost);

        act.Should().Throw<DomainException>().WithMessage("Reward choice cost cannot be negative.");
    }
}
