using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Application.Players.CreatePlayableCharacter;
using Leds.Player.Domain.Players;
using Moq;

namespace Leds.Player.UnitTests.Application.Players;

public sealed class CreatePlayableCharacterCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ShouldCreateArchetypedCharacterAndPersistProfile()
    {
        var profile = PlayerProfile.Create("Nocturne", Now.AddMinutes(-1));
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(r => r.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        var handler = new CreatePlayableCharacterCommandHandler(
            repository.Object, new FixedTimeProvider(Now), ArchetypeGateway());

        var result = await handler.Handle(
            new CreatePlayableCharacterCommand(profile.Id.Value, "Aster", "archetype.porteur"),
            CancellationToken.None);

        var character = result.Characters.Should().ContainSingle().Subject;
        character.DisplayName.Should().Be("Aster");
        character.ArchetypeKey.Should().Be("archetype.porteur");
        character.IsArchived.Should().BeFalse();
        repository.Verify(r => r.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProfileDoesNotExist()
    {
        var repository = new Mock<IPlayerProfileRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);
        var handler = new CreatePlayableCharacterCommandHandler(
            repository.Object, new FixedTimeProvider(Now), ArchetypeGateway());

        var act = () => handler.Handle(
            new CreatePlayableCharacterCommand(Guid.NewGuid(), "Aster", "archetype.porteur"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static IArchetypeDefinitionGateway ArchetypeGateway()
    {
        var gateway = new Mock<IArchetypeDefinitionGateway>();
        gateway.Setup(item => item.GetByKeyAsync("archetype.porteur", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchetypeDefinitionSnapshot(
                "archetype.porteur", PlayerCharacterStatBlock.CreateDefaultPorteur(), [], [],
                ["skill.basic.guard"], ["skill.basic.guard"]));
        return gateway.Object;
    }
}
