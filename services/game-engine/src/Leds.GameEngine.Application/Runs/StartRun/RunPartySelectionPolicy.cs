using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Application.Runs.StartRun;

public static class RunPartySelectionPolicy
{
    public static IReadOnlyCollection<PlayerRunSnapshotCharacter> Resolve(
        IReadOnlyCollection<PlayerRunSnapshotCharacter> snapshotCharacters,
        IReadOnlyCollection<PlayerCharacterView> profileCharacters,
        Guid? selectedCharacterId)
    {
        if (!selectedCharacterId.HasValue)
            return snapshotCharacters.ToArray();

        var selectedProfileCharacter = profileCharacters.FirstOrDefault(character =>
            character.Id == selectedCharacterId.Value
            && string.Equals(character.CharacterType, "Player", StringComparison.OrdinalIgnoreCase));
        var selectedSnapshot = snapshotCharacters.FirstOrDefault(character =>
            character.CharacterId == selectedCharacterId.Value);

        if (selectedProfileCharacter is null || selectedSnapshot is null)
        {
            throw new DomainException(
                "The selected character is unavailable or does not belong to the account.");
        }

        var companionIds = profileCharacters
            .Where(character => string.Equals(
                character.CharacterType, "Companion", StringComparison.OrdinalIgnoreCase))
            .Select(character => character.Id)
            .ToHashSet();

        return snapshotCharacters
            .Where(character => character.CharacterId == selectedCharacterId.Value
                || companionIds.Contains(character.CharacterId))
            .OrderByDescending(character => character.CharacterId == selectedCharacterId.Value)
            .ToArray();
    }
}
