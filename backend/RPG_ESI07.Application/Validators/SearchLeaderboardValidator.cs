using FluentValidation;
using RPG_ESI07.Application.Queries.Leaderboard;

namespace RPG_ESI07.Application.Validators; 
public class SearchLeaderboardQueryValidator : AbstractValidator<SearchLeaderboardQuery>
{
    public SearchLeaderboardQueryValidator()
    {
        RuleFor(x => x.Term)
            .NotEmpty()
            .MinimumLength(2)
                .WithMessage("La recherche doit contenir au moins 2 caractères.")
            .MaximumLength(50)
                .WithMessage("La recherche ne peut pas dépasser 50 caractères.")
            .Matches(@"^[\p{L}\p{N}\s_\-]+$")
                .WithMessage("Caractères non autorisés.");
    }
}
