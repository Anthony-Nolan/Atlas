using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    internal class MismatchCriteriaValidator : AbstractValidator<MismatchCriteria>
    {
        private const int MinimumMismatchCount = 0;

        // TODO ATLAS-865: make this configurable
        private const int MaximumTotalMismatchCount = 9;

        public MismatchCriteriaValidator()
        {
            RuleFor(x => x.LocusMismatchCriteria)
                .NotNull()
                .SetValidator(new LocusMismatchCriteriaValidator());

            RuleFor(x => x.DonorMismatchCount)
                .NotNull()
                .GreaterThanOrEqualTo(MinimumMismatchCount)
                .LessThanOrEqualTo(MaximumTotalMismatchCount);
        }
    }

    internal class LocusMismatchCriteriaValidator : AbstractValidator<LociInfoTransfer<int?>>
    {
        private const int MinimumMismatchCount = 0;
        private const int MaximumLocusMismatchCount = 2;

        public LocusMismatchCriteriaValidator()
        {
            RuleFor(x => x.A)
                .NotNull()
                .GreaterThanOrEqualTo(MinimumMismatchCount)
                .LessThanOrEqualTo(MaximumLocusMismatchCount);

            RuleFor(x => x.B)
                .NotNull()
                .GreaterThanOrEqualTo(MinimumMismatchCount)
                .LessThanOrEqualTo(MaximumLocusMismatchCount);

            RuleFor(x => x.Drb1)
                .NotNull()
                .GreaterThanOrEqualTo(MinimumMismatchCount)
                .LessThanOrEqualTo(MaximumLocusMismatchCount);

            RuleFor(x => x.C)
                .NotNull()
                .When(x => x.C != null)
                .GreaterThanOrEqualTo(MinimumMismatchCount)
                .LessThanOrEqualTo(MaximumLocusMismatchCount);

            RuleFor(x => x.Dqb1)
                .NotNull()
                .When(x => x.Dqb1 != null)
                .GreaterThanOrEqualTo(MinimumMismatchCount)
                .LessThanOrEqualTo(MaximumLocusMismatchCount);

            RuleFor(x => x.Dpb1).Null();
        }
    }
}