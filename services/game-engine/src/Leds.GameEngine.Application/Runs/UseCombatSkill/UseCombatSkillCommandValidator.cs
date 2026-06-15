using FluentValidation;

namespace Leds.GameEngine.Application.Runs.UseCombatSkill;

public sealed class UseCombatSkillCommandValidator : AbstractValidator<UseCombatSkillCommand>
{
    public UseCombatSkillCommandValidator()
    {
        RuleFor(command => command.RunId)
            .NotEmpty()
            .WithMessage("Run id is required.");

        RuleFor(command => command.CombatId)
            .NotEmpty()
            .WithMessage("Combat id is required.");

        RuleFor(command => command.ActorId)
            .NotEmpty()
            .WithMessage("Actor id is required.");

        RuleFor(command => command.SkillKey)
            .NotEmpty()
            .WithMessage("Skill key is required.");

        RuleFor(command => command.TargetIds)
            .NotNull()
            .WithMessage("Target ids are required.")
            .NotEmpty()
            .WithMessage("At least one target id is required.");

        RuleFor(command => command.TargetIds)
            .Must(targets => targets.Distinct().Count() == targets.Count)
            .WithMessage("Target ids must not contain duplicates.")
            .When(command => command.TargetIds is not null);
    }
}