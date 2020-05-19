using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    public class LocusSearchHlaValidator : AbstractValidator<LocusSearchHla>
    {
        public LocusSearchHlaValidator()
        {
            RuleFor(x => x.SearchHla1).NotEmpty();
            RuleFor(x => x.SearchHla2).NotEmpty();
        }
    }
}