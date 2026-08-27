using FluentAssertions;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;

namespace Leds.Catalog.UnitTests.Domain.Gameplay;

public sealed class GameplayDefinitionModelsCoverageTests
{
    [Fact]
    public void Catalog_version_should_trim_values_and_reject_missing_contract_fields()
    {
        var version = CatalogVersion.Create(" seed.v1 ", " 1.0 ");

        version.SeedKey.Should().Be("seed.v1");
        version.Version.Should().Be("1.0");
        new CatalogVersion().Should().Be(new CatalogVersion(string.Empty, string.Empty));

        Action missingSeed = () => CatalogVersion.Create(" ", "1.0");
        Action missingVersion = () => CatalogVersion.Create("seed", " ");
        missingSeed.Should().Throw<DomainException>().WithMessage("Catalog seed key is required.");
        missingVersion.Should().Throw<DomainException>().WithMessage("Catalog version is required.");
    }

    [Fact]
    public void Catalog_tag_should_normalize_optional_category_and_validate_required_values()
    {
        CatalogTag.Create(" tag.alpha ", " Alpha ", " combat ")
            .Should().Be(new CatalogTag("tag.alpha", "Alpha", "combat"));
        CatalogTag.Create("tag.beta", "Beta", " ").Category.Should().BeNull();

        Action missingKey = () => CatalogTag.Create("", "Alpha");
        Action missingName = () => CatalogTag.Create("tag", "");
        missingKey.Should().Throw<DomainException>();
        missingName.Should().Throw<DomainException>();
    }

    [Fact]
    public void Effect_models_should_order_effects_normalize_optional_values_and_validate_contract()
    {
        var first = EffectDefinition.Create(
            " Heal ", " Self ", 10, " Flat ", " Immediate ", " None ", 1,
            behaviorTag: " behavior ", generationTag: " ", selectionGroup: " group ", condition: " condition ");
        var second = EffectDefinition.Create("Guard", "Self", 5, "Flat", "Immediate", "None", 0);

        var set = EffectSet.Create(" effect.set ", " Set ", " description ", " 1.0 ", [first, second]);

        set.Key.Should().Be("effect.set");
        set.DisplayName.Should().Be("Set");
        set.Description.Should().Be("description");
        set.Effects.Should().ContainInOrder(second, first);
        first.BehaviorTag.Should().Be("behavior");
        first.GenerationTag.Should().BeNull();
        first.SelectionGroup.Should().Be("group");
        first.Condition.Should().Be("condition");

        Action noSetKey = () => EffectSet.Create("", "Set", null, "1", []);
        Action noSetName = () => EffectSet.Create("set", "", null, "1", []);
        Action noSetVersion = () => EffectSet.Create("set", "Set", null, "", []);
        Action noType = () => EffectDefinition.Create("", "Self", 1, "Flat", "Immediate", "None", 0);
        Action noTarget = () => EffectDefinition.Create("Heal", "", 1, "Flat", "Immediate", "None", 0);
        Action noValueMode = () => EffectDefinition.Create("Heal", "Self", 1, "", "Immediate", "None", 0);
        Action noDuration = () => EffectDefinition.Create("Heal", "Self", 1, "Flat", "", "None", 0);
        Action noStack = () => EffectDefinition.Create("Heal", "Self", 1, "Flat", "Immediate", "", 0);

        noSetKey.Should().Throw<DomainException>();
        noSetName.Should().Throw<DomainException>();
        noSetVersion.Should().Throw<DomainException>();
        noType.Should().Throw<DomainException>();
        noTarget.Should().Throw<DomainException>();
        noValueMode.Should().Throw<DomainException>();
        noDuration.Should().Throw<DomainException>();
        noStack.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reward_models_should_require_options_and_normalize_payload()
    {
        var option = RewardTemplateOption.Create(" Currency ", " Gold ", " Reward ", " gold ");
        var noPayload = RewardTemplateOption.Create("Currency", "Gold", "Reward", " ");
        var template = RewardTemplate.Create(" reward.default ", " Default ", [option]);

        option.Should().Be(new RewardTemplateOption("Currency", "Gold", "Reward", "gold"));
        noPayload.PayloadKey.Should().BeNull();
        template.Key.Should().Be("reward.default");
        template.DisplayName.Should().Be("Default");
        template.Options.Should().ContainSingle().Which.Should().Be(option);

        Action emptyOptions = () => RewardTemplate.Create("reward", "Reward", []);
        Action missingTemplateKey = () => RewardTemplate.Create("", "Reward", [option]);
        Action missingTemplateName = () => RewardTemplate.Create("reward", "", [option]);
        Action missingType = () => RewardTemplateOption.Create("", "Gold", "Reward");
        Action missingLabel = () => RewardTemplateOption.Create("Currency", "", "Reward");
        Action missingDescription = () => RewardTemplateOption.Create("Currency", "Gold", "");

        emptyOptions.Should().Throw<DomainException>();
        missingTemplateKey.Should().Throw<DomainException>();
        missingTemplateName.Should().Throw<DomainException>();
        missingType.Should().Throw<DomainException>();
        missingLabel.Should().Throw<DomainException>();
        missingDescription.Should().Throw<DomainException>();
    }

    [Fact]
    public void Curse_definition_should_trim_values_and_reject_invalid_contract_fields()
    {
        var curse = CurseDefinition.Create(
            " curse.coverage ", " Coverage curse ", " Coverage description ", 2, " Run ", " 1.0 ");

        curse.Should().Be(new CurseDefinition(
            "curse.coverage", "Coverage curse", "Coverage description", 2, "Run", "1.0"));

        Action missingKey = () => CurseDefinition.Create(" ", "Curse", "Desc", 1, "Run", "1");
        Action missingName = () => CurseDefinition.Create("curse", " ", "Desc", 1, "Run", "1");
        Action missingDescription = () => CurseDefinition.Create("curse", "Curse", " ", 1, "Run", "1");
        Action missingDuration = () => CurseDefinition.Create("curse", "Curse", "Desc", 1, " ", "1");
        Action missingVersion = () => CurseDefinition.Create("curse", "Curse", "Desc", 1, "Run", " ");
        Action invalidSeverity = () => CurseDefinition.Create("curse", "Curse", "Desc", 0, "Run", "1");

        missingKey.Should().Throw<DomainException>();
        missingName.Should().Throw<DomainException>();
        missingDescription.Should().Throw<DomainException>();
        missingDuration.Should().Throw<DomainException>();
        missingVersion.Should().Throw<DomainException>();
        invalidSeverity.Should().Throw<DomainException>();
    }

    [Fact]
    public void Lightweight_gameplay_links_should_trim_and_reject_blank_keys()
    {
        EnemySkillLink.Create(" skill.basic ").SkillDefinitionKey.Should().Be("skill.basic");
        EnemyTag.Create(" enemy.tag ").TagKey.Should().Be("enemy.tag");

        Action missingSkill = () => EnemySkillLink.Create(" ");
        Action missingTag = () => EnemyTag.Create(" ");
        missingSkill.Should().Throw<DomainException>();
        missingTag.Should().Throw<DomainException>();
    }
}
