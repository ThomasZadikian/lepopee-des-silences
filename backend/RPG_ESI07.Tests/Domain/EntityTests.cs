using FluentAssertions;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Tests.Domain;

public class EnemyEntityTests
{
    [Fact]
    public void Enemy_DefaultValues_AreCorrect()
    {
        // Act
        var enemy = new Enemy();

        // Assert
        enemy.Type.Should().Be("basic");
        enemy.PhysicalResistance.Should().Be(1.0f);
        enemy.MagicalResistance.Should().Be(1.0f);
        enemy.BestiaryUnlocks.Should().BeEmpty();
    }

    [Fact]
    public void Enemy_CanSetAllProperties()
    {
        // Arrange & Act
        var enemy = new Enemy
        {
            Name = "Dragon",
            Type = "boss",
            MaxHP = 500,
            Strength = 30,
            Intelligence = 25,
            Speed = 15,
            ExperienceReward = 500,
            GoldReward = 200
        };

        // Assert
        enemy.Name.Should().Be("Dragon");
        enemy.Type.Should().Be("boss");
        enemy.MaxHP.Should().Be(500);
    }
}

public class PlayerProfileEntityTests
{
    [Fact]
    public void PlayerProfile_DefaultValues_AreCorrect()
    {
        // Act
        var profile = new PlayerProfile();

        // Assert
        profile.Level.Should().Be(1);
        profile.CurrentHP.Should().Be(100);
        profile.MaxHP.Should().Be(100);
        profile.CurrentMP.Should().Be(50);
        profile.MaxMP.Should().Be(50);
        profile.Strength.Should().Be(10);
        profile.Intelligence.Should().Be(10);
        profile.Speed.Should().Be(10);
        profile.Experience.Should().Be(0);
        profile.Gold.Should().Be(0);
    }

    [Fact]
    public void PlayerProfile_NavigationCollections_AreInitialized()
    {
        // Act
        var profile = new PlayerProfile();

        // Assert
        profile.GameSaves.Should().NotBeNull().And.BeEmpty();
        profile.BestiaryUnlocks.Should().NotBeNull().And.BeEmpty();
        profile.Inventory.Should().NotBeNull().And.BeEmpty();
        profile.Skills.Should().NotBeNull().And.BeEmpty();
    }
}

public class UserEntityTests
{
    [Fact]
    public void User_DefaultValues_AreCorrect()
    {
        // Act
        var user = new User();

        // Assert
        user.MfaEnabled.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.AuditLogs.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_SoftDelete_CanBeSet()
    {
        // Arrange & Act
        var user = new User
        {
            DeletedAt = DateTime.UtcNow,
            DeletionReason = "RGPD request"
        };

        // Assert
        user.DeletedAt.Should().NotBeNull();
        user.DeletionReason.Should().Be("RGPD request");
    }
}

public class ItemEntityTests
{
    [Fact]
    public void Item_DefaultValues_AreCorrect()
    {
        // Act
        var item = new Item();

        // Assert
        item.Price.Should().Be(0);
        item.PlayerInventories.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("weapon")]
    [InlineData("armor")]
    [InlineData("accessory")]
    [InlineData("consumable")]
    public void Item_ValidTypes_CanBeAssigned(string type)
    {
        // Act
        var item = new Item { Type = type };

        // Assert
        item.Type.Should().Be(type);
    }
}

public class SkillEntityTests
{
    [Fact]
    public void Skill_DefaultValues_AreCorrect()
    {
        // Act
        var skill = new Skill();

        // Assert
        skill.PlayerSkills.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("damage")]
    [InlineData("heal")]
    [InlineData("buff")]
    [InlineData("debuff")]
    public void Skill_ValidEffectTypes_CanBeAssigned(string effectType)
    {
        // Act
        var skill = new Skill { EffectType = effectType };

        // Assert
        skill.EffectType.Should().Be(effectType);
    }
}

public class CombatStatsEntityTests
{
    [Fact]
    public void CombatStats_DefaultValues_AreZero()
    {
        // Act
        var stats = new CombatStats();

        // Assert
        stats.TotalCombats.Should().Be(0);
        stats.CombatsWon.Should().Be(0);
        stats.CombatsLost.Should().Be(0);
        stats.TotalDamageDealt.Should().Be(0);
        stats.TotalDamageTaken.Should().Be(0);
        stats.TotalPlaytimeMinutes.Should().Be(0);
    }
}

public class UserConsentEntityTests
{
    [Fact]
    public void UserConsent_DefaultValues_AreFalse()
    {
        // Act
        var consent = new UserConsent();

        // Assert
        consent.AnalyticsConsent.Should().BeFalse();
        consent.MarketingConsent.Should().BeFalse();
    }
}

public class GameSaveEntityTests
{
    [Fact]
    public void GameSave_SavedAt_DefaultsToUtcNow()
    {
        // Act
        var before = DateTime.UtcNow;
        var save = new GameSave();
        var after = DateTime.UtcNow;

        // Assert
        save.SavedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}