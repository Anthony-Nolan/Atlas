using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Validation;
using EnumStringValues;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Validators.SearchRequest
{
    public class SearchRequestValidator : AbstractValidator<Atlas.Client.Models.Search.Requests.SearchRequest>
    {
        private string Message_LocusCannotBeNullWhenCriteriaPresent(Locus locus) =>
            $"HLA Data at {locus} must be specified when matching criteria provided";

        public SearchRequestValidator()
        {
            RuleFor(x => x.SearchDonorType).NotNull().IsInEnum();
            RuleFor(x => x.MatchCriteria).NotNull().SetValidator(new MismatchCriteriaValidator());
            RuleFor(x => x.ScoringCriteria).NotNull().SetValidator(new ScoringCriteriaValidator());
            RuleFor(x => x.SearchHlaData).NotNull();
            RuleFor(x => x.SearchHlaData)
                .SetValidator(x => new DynamicLocusRequiredValidator(
                    Message_LocusCannotBeNullWhenCriteriaPresent,
                    EnumExtensions.EnumerateValues<Locus>()
                        .Where(l => x?.MatchCriteria?.LocusMismatchCriteria?.ToLociInfo()?.GetLocus(l) != null).ToList()
                )).When(x => x.SearchHlaData != null);
        }
    }

    internal class DynamicLocusRequiredValidator : PhenotypeHlaNamesValidator
    {
        public DynamicLocusRequiredValidator(Func<Locus, string> messageFactory, IEnumerable<Locus> loci)
        {
            foreach (var locus in loci)
            {
                var message = messageFactory(locus);

                switch (locus)
                {
                    case Locus.A:
                        RuleFor(x => x.A)
                            .NotNull()
                            .WithMessage(message)
                            .SetValidator(new RequiredLocusHlaNamesValidator(message));
                        break;
                    case Locus.B:
                        RuleFor(x => x.B)
                            .NotNull()
                            .WithMessage(message)
                            .SetValidator(new RequiredLocusHlaNamesValidator(message));
                        break;
                    case Locus.C:
                        RuleFor(x => x.C)
                            .NotNull()
                            .WithMessage(message)
                            .SetValidator(new RequiredLocusHlaNamesValidator(message));
                        break;
                    case Locus.Dpb1:
                        RuleFor(x => x.Dpb1)
                            .NotNull()
                            .WithMessage(message)
                            .SetValidator(new RequiredLocusHlaNamesValidator(message));
                        break;
                    case Locus.Dqb1:
                        RuleFor(x => x.Dqb1)
                            .NotNull()
                            .WithMessage(message)
                            .SetValidator(new RequiredLocusHlaNamesValidator(message));
                        break;
                    case Locus.Drb1:
                        RuleFor(x => x.Drb1)
                            .NotNull()
                            .WithMessage(message)
                            .SetValidator(new RequiredLocusHlaNamesValidator(message));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
                }
            }
        }
    }
}