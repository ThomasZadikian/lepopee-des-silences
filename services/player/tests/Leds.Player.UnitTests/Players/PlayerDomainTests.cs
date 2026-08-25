using FluentAssertions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;

namespace Leds.Player.UnitTests.Players;

public sealed class PlayerProfileTests
{
    [Fact]
    public void Create_ShouldCreateDefaultCharacter()
    {
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);

        profile.Roster.Characters.Should().HaveCount(1);
        profile.Roster.Characters.Single().DefinitionKey.Should().Be("character.player.self");
        profile.Roster.Characters.Single().DisplayName.Should().Be("L'Aventurier");
    }

    [Fact]
    public void Create_ShouldRejectEmptyDisplayName()
    {
        var act = () => PlayerProfile.Create("", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*display name*");
    }

    [Fact]
    public void Create_ShouldInitializeProgressionAtZero()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        profile.Progression.TotalRunsStarted.Should().Be(0);
        profile.Progression.TotalRunsCompleted.Should().Be(0);
        profile.Progression.TotalRunsFailed.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldStartDefaultCharacterWithStarterSkillsEquipped()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var character = profile.Roster.Characters.Single();

        character.EquippedSkillKeys.Should().BeEquivalentTo(["skill.basic.strike", "skill.basic.guard"]);
    }

    [Fact]
    public void AwardStatPoint_ShouldIncrementProgressionAndTouchProfile()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test", createdAt);
        var awardedAt = createdAt.AddMinutes(5);

        profile.AwardStatPoint(awardedAt);

        profile.Progression.UnspentStatPoints.Should().Be(1);
        profile.UpdatedAtUtc.Should().Be(awardedAt);
    }

    [Fact]
    public void AwardStatPoint_ShouldAwardTheGivenAmount()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        profile.AwardStatPoint(DateTimeOffset.UtcNow, amount: 5);

        profile.Progression.UnspentStatPoints.Should().Be(5);
        profile.Progression.TotalStatPointsEarned.Should().Be(5);
    }

    [Fact]
    public void AwardCurrency_ShouldIncrementProgressionAndTouchProfile()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test", createdAt);
        var awardedAt = createdAt.AddMinutes(5);

        profile.AwardCurrency(awardedAt, 1000);

        profile.Progression.PalaceShardCount.Should().Be(1000);
        profile.UpdatedAtUtc.Should().Be(awardedAt);
    }

    [Fact]
    public void AwardCurrency_ShouldAccumulateAcrossMultipleAwards()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        profile.AwardCurrency(DateTimeOffset.UtcNow, 1000);
        profile.AwardCurrency(DateTimeOffset.UtcNow, 500);

        profile.Progression.PalaceShardCount.Should().Be(1500);
    }

    [Fact]
    public void AwardCurrency_ShouldRejectNonPositiveAmount()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        var act = () => profile.AwardCurrency(DateTimeOffset.UtcNow, 0);

        act.Should().Throw<DomainException>().WithMessage("*Currency amount*");
    }

    [Fact]
    public void TrySpendCurrency_ShouldDecrementAndReturnTrue_WhenAffordable()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test", createdAt);
        profile.AwardCurrency(createdAt, 100);
        var spentAt = createdAt.AddMinutes(5);

        var succeeded = profile.TrySpendCurrency(spentAt, 30);

        succeeded.Should().BeTrue();
        profile.Progression.PalaceShardCount.Should().Be(70);
        profile.UpdatedAtUtc.Should().Be(spentAt);
    }

    [Fact]
    public void TrySpendCurrency_ShouldReturnFalseAndLeaveBalanceUnchanged_WhenInsufficientFunds()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test", createdAt);
        profile.AwardCurrency(createdAt, 10);

        var succeeded = profile.TrySpendCurrency(createdAt.AddMinutes(5), 30);

        succeeded.Should().BeFalse();
        profile.Progression.PalaceShardCount.Should().Be(10);
    }

    [Fact]
    public void TrySpendCurrency_ShouldRejectNonPositiveAmount()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        var act = () => profile.TrySpendCurrency(DateTimeOffset.UtcNow, 0);

        act.Should().Throw<DomainException>().WithMessage("*Currency amount*");
    }

    [Fact]
    public void AwardHimLitCurrency_ShouldIncrementProgressionAndTouchProfile()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test", createdAt);
        var awardedAt = createdAt.AddMinutes(5);

        profile.AwardHimLitCurrency(awardedAt, 25);

        profile.Progression.HimLitShardCount.Should().Be(25);
        profile.UpdatedAtUtc.Should().Be(awardedAt);
    }

    [Fact]
    public void AwardHimLitCurrency_ShouldAccumulateAcrossMultipleAwards()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        profile.AwardHimLitCurrency(DateTimeOffset.UtcNow, 25);
        profile.AwardHimLitCurrency(DateTimeOffset.UtcNow, 50);

        profile.Progression.HimLitShardCount.Should().Be(75);
    }

    [Fact]
    public void AwardHimLitCurrency_ShouldRejectNonPositiveAmount()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        var act = () => profile.AwardHimLitCurrency(DateTimeOffset.UtcNow, 0);

        act.Should().Throw<DomainException>().WithMessage("*Currency amount*");
    }

    [Fact]
    public void TrySpendHimLitCurrency_ShouldDecrementAndReturnTrue_WhenAffordable()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test", createdAt);
        profile.AwardHimLitCurrency(createdAt, 100);
        var spentAt = createdAt.AddMinutes(5);

        var succeeded = profile.TrySpendHimLitCurrency(spentAt, 30);

        succeeded.Should().BeTrue();
        profile.Progression.HimLitShardCount.Should().Be(70);
        profile.UpdatedAtUtc.Should().Be(spentAt);
    }

    [Fact]
    public void TrySpendHimLitCurrency_ShouldReturnFalseAndLeaveBalanceUnchanged_WhenInsufficientFunds()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var profile = PlayerProfile.Create("Test", createdAt);
        profile.AwardHimLitCurrency(createdAt, 10);

        var succeeded = profile.TrySpendHimLitCurrency(createdAt.AddMinutes(5), 30);

        succeeded.Should().BeFalse();
        profile.Progression.HimLitShardCount.Should().Be(10);
    }

    [Fact]
    public void TrySpendHimLitCurrency_ShouldRejectNonPositiveAmount()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        var act = () => profile.TrySpendHimLitCurrency(DateTimeOffset.UtcNow, 0);

        act.Should().Throw<DomainException>().WithMessage("*Currency amount*");
    }

    [Fact]
    public void SpendStatPoint_ShouldRejectWhenNoPointsAvailable()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var characterId = profile.Roster.Characters.Single().Id;

        var act = () => profile.SpendStatPoint(characterId, PlayerStatKind.AttackPower, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*No stat points*");
    }

    [Fact]
    public void SpendStatPoint_ShouldRejectUnknownCharacterWithoutConsumingThePoint()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        profile.AwardStatPoint(DateTimeOffset.UtcNow);

        var act = () => profile.SpendStatPoint(PlayerCharacterId.New(), PlayerStatKind.AttackPower, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*not found*");
        profile.Progression.UnspentStatPoints.Should().Be(1);
    }

    [Fact]
    public void SpendStatPoint_ShouldApplyIncrementAndDecrementUnspentPoints()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var character = profile.Roster.Characters.Single();
        profile.AwardStatPoint(DateTimeOffset.UtcNow);
        var originalAttackPower = character.StatBlock.AttackPower;

        profile.SpendStatPoint(character.Id, PlayerStatKind.AttackPower, DateTimeOffset.UtcNow);

        character.StatBlock.AttackPower.Should().Be(originalAttackPower + 1);
        profile.Progression.UnspentStatPoints.Should().Be(0);
        profile.Progression.TotalStatPointsEarned.Should().Be(1);
    }

    [Fact]
    public void SpendStatPoint_ShouldIncrementStatPointsInvestedOnTheCharacter()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var character = profile.Roster.Characters.Single();
        profile.AwardStatPoint(DateTimeOffset.UtcNow, amount: 2);

        profile.SpendStatPoint(character.Id, PlayerStatKind.AttackPower, DateTimeOffset.UtcNow);
        profile.SpendStatPoint(character.Id, PlayerStatKind.Defense, DateTimeOffset.UtcNow);

        character.StatPointsInvested.Should().Be(2);
    }

    [Fact]
    public void LearnSkill_ShouldAddTheSkillToTheCharacterWithTheGivenSource()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var character = profile.Roster.Characters.Single();
        var now = DateTimeOffset.UtcNow;

        profile.LearnSkill(character.Id, "skill.new", "devtools", now);

        var learned = character.Skills.Single(s => s.SkillDefinitionKey == "skill.new");
        learned.Source.Should().Be("devtools");
        learned.IsEquipped.Should().BeFalse();
        profile.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void LearnSkill_ShouldRejectUnknownCharacter()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        var act = () => profile.LearnSkill(PlayerCharacterId.New(), "skill.new", "devtools", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*not found*");
    }

    [Fact]
    public void EquipSkill_ShouldDelegateToTheCharacter()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var character = profile.Roster.Characters.Single();
        character.LearnSkill(PlayerCharacterSkill.Create("skill.new", DateTimeOffset.UtcNow));

        profile.EquipSkill(character.Id, "skill.new", DateTimeOffset.UtcNow);

        character.EquippedSkillKeys.Should().Contain("skill.new");
    }

    [Fact]
    public void EquipSkill_ShouldRejectUnknownCharacter()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        var act = () => profile.EquipSkill(PlayerCharacterId.New(), "skill.basic.strike", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*not found*");
    }

    [Fact]
    public void GrantPermanentUnlock_ShouldAddUnlock_WhenNotAlreadyGranted()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;

        profile.GrantPermanentUnlock("npc.hitomi:offer.skill", "npc-offering", sourceRunId: null, now);

        profile.PermanentUnlocks.Should().ContainSingle();
        profile.HasPermanentUnlock("npc.hitomi:offer.skill").Should().BeTrue();
    }

    [Fact]
    public void GrantPermanentUnlock_ShouldBeIdempotent_WhenCalledTwiceWithSameKey()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;

        profile.GrantPermanentUnlock("npc.hitomi:offer.skill", "npc-offering", sourceRunId: null, now);
        profile.GrantPermanentUnlock("npc.hitomi:offer.skill", "npc-offering", sourceRunId: null, now);

        profile.PermanentUnlocks.Should().ContainSingle();
    }

    [Fact]
    public void GrantPermanentUnlock_ShouldTrackMultipleNpcsIndependently()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;

        profile.GrantPermanentUnlock("npc.hitomi:offer.skill", "npc-offering", sourceRunId: null, now);
        profile.GrantPermanentUnlock("npc.elyas:milestone.trust", "npc-reputation-milestone", sourceRunId: null, now);

        profile.PermanentUnlocks.Should().HaveCount(2);
        profile.HasPermanentUnlock("npc.hitomi:offer.skill").Should().BeTrue();
        profile.HasPermanentUnlock("npc.elyas:milestone.trust").Should().BeTrue();
        profile.HasPermanentUnlock("npc.owen:milestone.trust").Should().BeFalse();
    }

    [Fact]
    public void RecruitCompanion_ShouldAddCharacterToRoster_WhenNotAlreadyRecruited()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var statBlock = PlayerCharacterStatBlock.Create(
            maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
            speed: 10, initiative: 10,focus: 0, mana: 0, charge: 0);

        profile.RecruitCompanion("character.thomas", "Thomas", statBlock, ["skill.basic.guard"], now);

        profile.Roster.Characters.Should().HaveCount(2);
        var companion = profile.Roster.Characters.Single(c => c.DefinitionKey == "character.thomas");
        companion.DisplayName.Should().Be("Thomas");
        companion.CharacterType.Should().Be("Companion");
        companion.EquippedSkillKeys.Should().Contain("skill.basic.guard");
    }

    [Fact]
    public void RecruitCompanion_ShouldBeIdempotent_WhenCalledTwiceWithSameKey()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var statBlock = PlayerCharacterStatBlock.Create(
            maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
            speed: 10, initiative: 10,focus: 0, mana: 0, charge: 0);

        profile.RecruitCompanion("character.thomas", "Thomas", statBlock, ["skill.basic.guard"], now);
        profile.RecruitCompanion("character.thomas", "Thomas", statBlock, ["skill.basic.guard"], now);

        profile.Roster.Characters.Should().HaveCount(2);
    }

    [Fact]
    public void RecruitCompanion_ShouldNotGrantPermanentStatPoints_WhenAnExistingCharacterHasInvestedPoints()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var protagonist = profile.Roster.Characters.Single();
        profile.AwardStatPoint(now, amount: 3);
        profile.SpendStatPoint(protagonist.Id, PlayerStatKind.AttackPower, now);
        profile.SpendStatPoint(protagonist.Id, PlayerStatKind.Defense, now);
        // 1 point left unspent going into recruitment — must not be confused with the catch-up grant.
        var statBlock = PlayerCharacterStatBlock.Create(
            maxVitality: 85, attackPower: 15, defense: 4, startingGuard: 0,
            speed: 13, initiative: 12,focus: 3, mana: 15, charge: 0);

        profile.RecruitCompanion("character.mane", "Mané", statBlock, ["skill.basic.strike"], now);

        profile.Progression.UnspentStatPoints.Should().Be(1);
        profile.Progression.TotalStatPointsEarned.Should().Be(3);
    }

    [Fact]
    public void RecruitCompanion_ShouldNotGrantCatchUpStatPoints_WhenNoCharacterHasInvestedAnyYet()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var statBlock = PlayerCharacterStatBlock.Create(
            maxVitality: 85, attackPower: 15, defense: 4, startingGuard: 0,
            speed: 13, initiative: 12,focus: 3, mana: 15, charge: 0);

        profile.RecruitCompanion("character.mane", "Mané", statBlock, ["skill.basic.strike"], now);

        profile.Progression.UnspentStatPoints.Should().Be(0);
        profile.Progression.TotalStatPointsEarned.Should().Be(0);
    }

    [Fact]
    public void RecruitCompanion_ShouldNotGrantPermanentStatPoints_ForAnAdvancedExistingCompanion()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var protagonist = profile.Roster.Characters.Single();
        var firstCompanionStatBlock = PlayerCharacterStatBlock.Create(
            maxVitality: 110, attackPower: 8, defense: 12, startingGuard: 8,
            speed: 8, initiative: 8,focus: 2, mana: 14, charge: 0);
        profile.RecruitCompanion("character.thomas", "Thomas", firstCompanionStatBlock, ["skill.basic.guard"], now);
        var thomas = profile.Roster.Characters.Single(c => c.DefinitionKey == "character.thomas");
        profile.AwardStatPoint(now, amount: 4);
        profile.SpendStatPoint(thomas.Id, PlayerStatKind.AttackPower, now);
        profile.SpendStatPoint(thomas.Id, PlayerStatKind.AttackPower, now);
        profile.SpendStatPoint(thomas.Id, PlayerStatKind.AttackPower, now);
        // Thomas now has 3 points invested, well ahead of the still-untouched protagonist.
        var secondCompanionStatBlock = PlayerCharacterStatBlock.Create(
            maxVitality: 85, attackPower: 15, defense: 4, startingGuard: 0,
            speed: 13, initiative: 12,focus: 3, mana: 15, charge: 0);

        profile.RecruitCompanion("character.mane", "Mané", secondCompanionStatBlock, ["skill.basic.strike"], now);

        profile.Progression.UnspentStatPoints.Should().Be(1);
        protagonist.StatPointsInvested.Should().Be(0);
    }

    [Fact]
    public void AddPermanentItems_ShouldAddNewItemsOnly()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;

        profile.AddPermanentItems(["item.sac-a-dos", "item.amulette"], sourceRunId: null, now);
        profile.AddPermanentItems(["item.sac-a-dos"], sourceRunId: null, now);

        profile.PermanentItems.Should().HaveCount(2);
        profile.HasPermanentItem("item.sac-a-dos").Should().BeTrue();
        profile.HasPermanentItem("item.amulette").Should().BeTrue();
    }

    [Fact]
    public void EquipItem_ShouldAddItemToCharacterAndEquipIt_WhenOwnedInPermanentBackpack()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var character = profile.Roster.Characters.Single();
        var now = DateTimeOffset.UtcNow;
        profile.AddPermanentItems(["item.sac-a-dos"], sourceRunId: null, now);

        profile.EquipItem(character.Id, "item.sac-a-dos", now);

        character.EquippedItemKeys.Should().Contain("item.sac-a-dos");
    }

    [Fact]
    public void EquipItem_ShouldReject_WhenItemNotInPermanentBackpack()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var character = profile.Roster.Characters.Single();

        var act = () => profile.EquipItem(character.Id, "item.never-owned", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*not in the permanent backpack*");
    }

    [Fact]
    public void SetPermanentItemContent_ShouldSetContainedLiquid_WhenItemIsOwned()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        profile.AddPermanentItems(["item.fiole-cristal"], sourceRunId: null, now);

        profile.SetPermanentItemContent("item.fiole-cristal", "item.larme-de-racine", now);

        profile.PermanentItems.Single().ContainedLiquidDefinitionKey.Should().Be("item.larme-de-racine");
    }

    [Fact]
    public void SetPermanentItemContent_ShouldReject_WhenItemNotInPermanentBackpack()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);

        var act = () => profile.SetPermanentItemContent("item.never-owned", "item.larme-de-racine", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*not in the permanent backpack*");
    }

    [Fact]
    public void SetPermanentItemContent_ShouldReject_WhenAlreadyFilled()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        profile.AddPermanentItems(["item.fiole-cristal"], sourceRunId: null, now);
        profile.SetPermanentItemContent("item.fiole-cristal", "item.larme-de-racine", now);

        var act = () => profile.SetPermanentItemContent("item.fiole-cristal", "item.datura", now);

        act.Should().Throw<DomainException>().WithMessage("*already holds a liquid*");
    }

    [Fact]
    public void ClearPermanentItemContent_ShouldEmptyContainer()
    {
        var profile = PlayerProfile.Create("Test", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        profile.AddPermanentItems(["item.fiole-cristal"], sourceRunId: null, now);
        profile.SetPermanentItemContent("item.fiole-cristal", "item.larme-de-racine", now);

        profile.ClearPermanentItemContent("item.fiole-cristal", now);

        profile.PermanentItems.Single().ContainedLiquidDefinitionKey.Should().BeNull();
    }
}

public sealed class PlayerCharacterTests
{
    [Fact]
    public void Create_ShouldRejectInvalidVitality()
    {
        var act = () => PlayerCharacter.Create("key", "Name", 0, 0, 0, ["skill"]);
        act.Should().Throw<DomainException>().WithMessage("*Max vitality*");
    }

    [Fact]
    public void Create_ShouldRejectEmptySkillKeys()
    {
        var act = () => PlayerCharacter.Create("key", "Name", 100, 0, 0, []);
        act.Should().Throw<DomainException>().WithMessage("*at least one skill*");
    }

    private static PlayerCharacter CreateCharacterWithSkills(params string[] skillKeys)
    {
        var now = DateTimeOffset.UtcNow;
        var skills = skillKeys.Select(key => PlayerCharacterSkill.Create(key, now)).ToArray();

        return PlayerCharacter.Create(
            "key", "Name", PlayerCharacterStatBlock.CreateDefaultPorteur(), skills,
            status: "Active");
    }

    [Fact]
    public void EquipSkill_ShouldMarkSkillAsEquipped()
    {
        var character = CreateCharacterWithSkills("skill.a");

        character.EquipSkill("skill.a");

        character.Skills.Single().IsEquipped.Should().BeTrue();
        character.EquippedSkillKeys.Should().Contain("skill.a");
        character.EquippedSkillKeys.Should().Contain(PlayerCharacter.BasicSkillKey);
    }

    [Fact]
    public void EquipSkill_ShouldBeIdempotentWhenAlreadyEquipped()
    {
        var character = CreateCharacterWithSkills("skill.a");
        character.EquipSkill("skill.a");

        var act = () => character.EquipSkill("skill.a");

        act.Should().NotThrow();
        character.EquippedCount.Should().Be(1);
    }

    [Fact]
    public void EquipSkill_ShouldRejectUnknownSkill()
    {
        var character = CreateCharacterWithSkills("skill.a");

        var act = () => character.EquipSkill("skill.unknown");

        act.Should().Throw<DomainException>().WithMessage("*not known*");
    }

    [Fact]
    public void EquipSkill_ShouldRejectExceedingMaxEquippedSkills()
    {
        var character = CreateCharacterWithSkills("skill.a", "skill.b", "skill.c", "skill.d", "skill.e");
        character.EquipSkill("skill.a");
        character.EquipSkill("skill.b");
        character.EquipSkill("skill.c");
        character.EquipSkill("skill.d");

        var act = () => character.EquipSkill("skill.e");

        act.Should().Throw<DomainException>().WithMessage("*Cannot equip more than*");
        character.EquippedCount.Should().Be(4);
    }

    [Fact]
    public void UnequipSkill_ShouldFreeUpASlot()
    {
        var character = CreateCharacterWithSkills("skill.a", "skill.b", "skill.c", "skill.d", "skill.e");
        character.EquipSkill("skill.a");
        character.EquipSkill("skill.b");
        character.EquipSkill("skill.c");
        character.EquipSkill("skill.d");

        character.UnequipSkill("skill.a");
        var act = () => character.EquipSkill("skill.e");

        act.Should().NotThrow();
        character.EquippedCount.Should().Be(4);
        character.EquippedSkillKeys.Should().NotContain("skill.a");
    }

    [Fact]
    public void UnequipSkill_ShouldRejectUnknownSkill()
    {
        var character = CreateCharacterWithSkills("skill.a");

        var act = () => character.UnequipSkill("skill.unknown");

        act.Should().Throw<DomainException>().WithMessage("*not known*");
    }

    [Fact]
    public void EquipItem_ShouldMarkItemAsEquipped()
    {
        var character = CreateCharacterWithSkills("skill.a");
        character.AddItem(PlayerCharacterItem.Create("item.a", DateTimeOffset.UtcNow));

        character.EquipItem("item.a");

        character.Items.Single().IsEquipped.Should().BeTrue();
        character.EquippedItemKeys.Should().Contain("item.a");
    }

    [Fact]
    public void EquipItem_ShouldBeIdempotentWhenAlreadyEquipped()
    {
        var character = CreateCharacterWithSkills("skill.a");
        character.AddItem(PlayerCharacterItem.Create("item.a", DateTimeOffset.UtcNow));
        character.EquipItem("item.a");

        var act = () => character.EquipItem("item.a");

        act.Should().NotThrow();
        character.EquippedItemCount.Should().Be(1);
    }

    [Fact]
    public void EquipItem_ShouldRejectUnknownItem()
    {
        var character = CreateCharacterWithSkills("skill.a");

        var act = () => character.EquipItem("item.unknown");

        act.Should().Throw<DomainException>().WithMessage("*not owned*");
    }

    [Fact]
    public void EquipItem_ShouldRejectExceedingMaxEquippedItems()
    {
        var character = CreateCharacterWithSkills("skill.a");
        var now = DateTimeOffset.UtcNow;
        character.AddItem(PlayerCharacterItem.Create("item.a", now));
        character.AddItem(PlayerCharacterItem.Create("item.b", now));
        character.AddItem(PlayerCharacterItem.Create("item.c", now));
        character.AddItem(PlayerCharacterItem.Create("item.d", now));
        character.EquipItem("item.a");
        character.EquipItem("item.b");
        character.EquipItem("item.c");

        var act = () => character.EquipItem("item.d");

        act.Should().Throw<DomainException>().WithMessage("*Cannot equip more than*");
        character.EquippedItemCount.Should().Be(3);
    }

    [Fact]
    public void UnequipItem_ShouldFreeUpASlot()
    {
        var character = CreateCharacterWithSkills("skill.a");
        var now = DateTimeOffset.UtcNow;
        character.AddItem(PlayerCharacterItem.Create("item.a", now));
        character.AddItem(PlayerCharacterItem.Create("item.b", now));
        character.AddItem(PlayerCharacterItem.Create("item.c", now));
        character.AddItem(PlayerCharacterItem.Create("item.d", now));
        character.EquipItem("item.a");
        character.EquipItem("item.b");
        character.EquipItem("item.c");

        character.UnequipItem("item.a");
        var act = () => character.EquipItem("item.d");

        act.Should().NotThrow();
        character.EquippedItemCount.Should().Be(3);
        character.EquippedItemKeys.Should().NotContain("item.a");
    }

    [Fact]
    public void EquipItem_ShouldAllowOneWeaponOneAccessoryAndThreeRelics()
    {
        var character = CreateCharacterWithSkills("skill.a");
        foreach (var key in new[] { "weapon.a", "accessory.a", "relic.a", "relic.b", "relic.c" })
            character.AddItem(PlayerCharacterItem.Create(key, DateTimeOffset.UtcNow));

        character.EquipItem("weapon.a", EquipmentSlotKind.Weapon);
        character.EquipItem("accessory.a", EquipmentSlotKind.Accessory);
        character.EquipItem("relic.a", EquipmentSlotKind.Relic);
        character.EquipItem("relic.b", EquipmentSlotKind.Relic);
        character.EquipItem("relic.c", EquipmentSlotKind.Relic);

        character.EquippedItemCount.Should().Be(5);
    }

    [Fact]
    public void EquipItem_ShouldRejectASecondWeapon()
    {
        var character = CreateCharacterWithSkills("skill.a");
        character.AddItem(PlayerCharacterItem.Create("weapon.a", DateTimeOffset.UtcNow));
        character.AddItem(PlayerCharacterItem.Create("weapon.b", DateTimeOffset.UtcNow));
        character.EquipItem("weapon.a", EquipmentSlotKind.Weapon);

        var act = () => character.EquipItem("weapon.b", EquipmentSlotKind.Weapon);

        act.Should().Throw<DomainException>()
            .WithMessage("*slot Weapon*");
    }

    [Fact]
    public void ApplyStatIncrement_ShouldReplaceStatBlockWithIncrementedValue()
    {
        var character = CreateCharacterWithSkills("skill.a");
        var originalAttackPower = character.StatBlock.AttackPower;

        character.ApplyStatIncrement(PlayerStatKind.AttackPower);

        character.StatBlock.AttackPower.Should().Be(originalAttackPower + 1);
    }

    [Fact]
    public void ApplyStatIncrement_ShouldIncrementStatPointsInvested()
    {
        var character = CreateCharacterWithSkills("skill.a");

        character.ApplyStatIncrement(PlayerStatKind.AttackPower);
        character.ApplyStatIncrement(PlayerStatKind.Defense);

        character.StatPointsInvested.Should().Be(2);
    }

    [Fact]
    public void LearnSkill_ShouldDedupeLikeAddSkill()
    {
        var character = CreateCharacterWithSkills("skill.a");

        character.LearnSkill(PlayerCharacterSkill.Create("skill.a", DateTimeOffset.UtcNow));

        character.Skills.Should().HaveCount(1);
    }
}

public sealed class PlayerRosterTests
{
    [Fact]
    public void AddCharacter_ShouldRejectDuplicateCharacter()
    {
        var roster = PlayerRoster.Create();
        var character = PlayerCharacter.Create("key", "Name", 100, 0, 0, ["skill"]);
        roster.AddCharacter(character);
        var act = () => roster.AddCharacter(character);

        act.Should().Throw<DomainException>().WithMessage("*already exists*");
    }

    [Fact]
    public void AddCharacter_ShouldRejectDuplicateDefinitionKey()
    {
        var roster = PlayerRoster.Create();
        var character1 = PlayerCharacter.Create("key", "Name1", 100, 0, 0, ["skill"]);
        var character2 = PlayerCharacter.Create("key", "Name2", 100, 0, 0, ["skill"]);
        roster.AddCharacter(character1);
        var act = () => roster.AddCharacter(character2);

        act.Should().Throw<DomainException>().WithMessage("*definition key*");
    }
}

public sealed class PlayerProgressionTests
{
    [Fact]
    public void CreateDefault_ShouldStartAtZero()
    {
        var progression = PlayerProgression.CreateDefault();

        progression.TotalRunsStarted.Should().Be(0);
        progression.TotalRunsCompleted.Should().Be(0);
        progression.TotalRunsFailed.Should().Be(0);
        progression.UnspentStatPoints.Should().Be(0);
        progression.TotalStatPointsEarned.Should().Be(0);
    }

    [Fact]
    public void AwardStatPoint_ShouldIncrementBothUnspentAndTotalEarned()
    {
        var progression = PlayerProgression.CreateDefault();

        progression.AwardStatPoint();
        progression.AwardStatPoint();

        progression.UnspentStatPoints.Should().Be(2);
        progression.TotalStatPointsEarned.Should().Be(2);
    }

    [Fact]
    public void SpendStatPoint_ShouldDecrementOnlyUnspent()
    {
        var progression = PlayerProgression.CreateDefault();
        progression.AwardStatPoint();
        progression.AwardStatPoint();

        progression.SpendStatPoint();

        progression.UnspentStatPoints.Should().Be(1);
        progression.TotalStatPointsEarned.Should().Be(2);
    }

    [Fact]
    public void SpendStatPoint_ShouldRejectWhenNoPointsAvailable()
    {
        var progression = PlayerProgression.CreateDefault();

        var act = () => progression.SpendStatPoint();

        act.Should().Throw<DomainException>().WithMessage("*No stat points*");
    }
}

public sealed class PlayerProfileRehydrateTests
{
    [Fact]
    public void Rehydrate_ShouldRestoreState()
    {
        var id = PlayerId.New();
        var character = PlayerCharacter.Rehydrate(
            PlayerCharacterId.New(), "key", "Name", 100, 0, 0, ["skill"]);
        var roster = PlayerRoster.Rehydrate([character]);
        var progression = PlayerProgression.Rehydrate(5, 3, 2, 1);
        var now = DateTimeOffset.UtcNow;

        var profile = PlayerProfile.Rehydrate(id, "Test", roster, progression, now, now);

        profile.Id.Should().Be(id);
        profile.DisplayName.Should().Be("Test");
        profile.Roster.Characters.Should().HaveCount(1);
        profile.Progression.TotalRunsStarted.Should().Be(5);
    }
}
