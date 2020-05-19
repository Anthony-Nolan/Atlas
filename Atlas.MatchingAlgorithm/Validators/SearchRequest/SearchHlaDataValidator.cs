using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    public class SearchHlaDataValidator : AbstractValidator<SearchHlaData>
    {
        public SearchHlaDataValidator()
        {
            RuleFor(x => x.LocusSearchHlaA).NotNull().SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaB).NotNull().SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaDrb1).NotNull().SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaC).SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaDqb1).SetValidator(new LocusSearchHlaValidator());
            RuleFor(x => x.LocusSearchHlaDpb1).SetValidator(new LocusSearchHlaValidator());
        }
    }
}