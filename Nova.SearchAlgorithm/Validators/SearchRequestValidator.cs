using FluentValidation;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;

namespace Nova.SearchAlgorithm.Validators
{
    public class SearchRequestValidator : AbstractValidator<SearchRequest>
    {
        public SearchRequestValidator()
        {
            RuleFor(x => x.SearchType).NotEmpty().IsInEnum();
            RuleFor(x => x.MatchCriteria).NotEmpty();
            RuleFor(x => x.SearchHlaData).NotEmpty();
            RuleFor(x => x.SearchHlaData.LocusSearchHlaC).NotEmpty().When(x => x.MatchCriteria?.LocusMismatchC != null);
            RuleFor(x => x.SearchHlaData.LocusSearchHlaDqb1).NotEmpty().When(x => x.MatchCriteria?.LocusMismatchDqb1 != null);
            RuleFor(x => x.RegistriesToSearch).NotEmpty();
            RuleForEach(x => x.RegistriesToSearch).IsInEnum();
        }
    }
}