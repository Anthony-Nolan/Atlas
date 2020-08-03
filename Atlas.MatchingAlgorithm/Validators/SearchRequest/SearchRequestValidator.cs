using Atlas.Common.GeneticData;
using Atlas.Common.Validation;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    public class SearchRequestValidator : AbstractValidator<Client.Models.SearchRequests.MatchingRequest>
    {
        private string Message_LocusCannotBeNullWhenCriteriaPresent(Locus locus) =>
            $"HLA Data at {locus} must be specified when matching criteria provided";

        public SearchRequestValidator()
        {
            RuleFor(x => x.SearchType).NotNull().IsInEnum();
            RuleFor(x => x.MatchCriteria).NotNull().SetValidator(new MismatchCriteriaValidator());
            RuleFor(x => x.ScoringCriteria).NotNull().SetValidator(new ScoringCriteriaValidator());
            RuleFor(x => x.SearchHlaData).NotNull().SetValidator(new PhenotypeHlaNamesValidator());

            RuleFor(x => x.SearchHlaData.C.Position1)
                .NotNull()
                .When(x => x.MatchCriteria?.LocusMismatchCounts.C != null)
                .WithMessage(Message_LocusCannotBeNullWhenCriteriaPresent(Locus.C));

            RuleFor(x => x.SearchHlaData.C.Position2)
                .NotNull()
                .When(x => x.MatchCriteria?.LocusMismatchCounts.C != null)
                .WithMessage(Message_LocusCannotBeNullWhenCriteriaPresent(Locus.C));

            RuleFor(x => x.SearchHlaData.Dqb1.Position1)
                .NotNull()
                .When(x => x.MatchCriteria?.LocusMismatchCounts.Dqb1 != null)
                .WithMessage(Message_LocusCannotBeNullWhenCriteriaPresent(Locus.Dqb1));

            RuleFor(x => x.SearchHlaData.Dqb1.Position2)
                .NotNull()
                .When(x => x.MatchCriteria?.LocusMismatchCounts.Dqb1 != null)
                .WithMessage(Message_LocusCannotBeNullWhenCriteriaPresent(Locus.Dqb1));
        }
    }
}