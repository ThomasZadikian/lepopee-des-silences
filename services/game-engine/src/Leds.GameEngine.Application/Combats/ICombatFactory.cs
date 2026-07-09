using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Combats;

public interface ICombatFactory
{
    Combat CreateFromDraft(
        CombatEncounterDraft draft,
        PlayerRuntimeState? playerState = null,
        IReadOnlyCollection<RunModifier>? runModifiers = null,
        int attackPower = 0,
        int defense = 0,
        int speed = 10,
        PalaceRoomState palaceRoomState = PalaceRoomState.Neutral,
        int focus = 0,
        IReadOnlyDictionary<string, IReadOnlyList<SkillStatusEffectSpec>>? skillEffects = null,
        IReadOnlyDictionary<EmotionalType, int>? typedDamageReductions = null,
        int hitChanceBonusPercent = 0,
        int dotDurationReductionPercent = 0,
        int dotDamageReductionPercent = 0);

    Combat CreateFromDraft(
        CombatId combatId,
        CombatEncounterDraft draft,
        PlayerRuntimeState? playerState = null,
        IReadOnlyCollection<RunModifier>? runModifiers = null,
        int attackPower = 0,
        int defense = 0,
        int speed = 10,
        PalaceRoomState palaceRoomState = PalaceRoomState.Neutral,
        int focus = 0,
        IReadOnlyDictionary<string, IReadOnlyList<SkillStatusEffectSpec>>? skillEffects = null,
        IReadOnlyDictionary<EmotionalType, int>? typedDamageReductions = null,
        int hitChanceBonusPercent = 0,
        int dotDurationReductionPercent = 0,
        int dotDamageReductionPercent = 0);
}