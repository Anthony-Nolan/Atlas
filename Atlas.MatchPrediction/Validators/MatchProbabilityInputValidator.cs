using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.Validation;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using FluentValidation;

namespace Atlas.MatchPrediction.Validators
{
    internal class MatchProbabilityInputValidator : MatchProbabilityNonDonorValidator
    {
        public MatchProbabilityInputValidator()
        {
            RuleFor(i => i.DonorInput).NotNull().SetValidator(new MatchProbabilityDonorInputValidator());
        }
    }

    internal class MatchProbabilityDonorInputValidator : AbstractValidator<DonorInput>
    {
        public MatchProbabilityDonorInputValidator()
        {
            RuleFor(i => i.DonorHla).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
        }
    }

    /// <summary>
    /// This validator is used to validate a search request for the MPA before the matching algorithm has been run
    /// </summary>
    public class MatchProbabilityNonDonorValidator : AbstractValidator<SingleDonorMatchProbabilityInput>
    {
        private readonly List<Locus> requiredLoci = new List<Locus> {Locus.A, Locus.B, Locus.Drb1};
        public MatchProbabilityNonDonorValidator()
        {
            RuleFor(i => i.PatientHla).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
            RuleForEach(i => i.ExcludedLoci).Must(l => !requiredLoci.Contains(l));
        }
        
    }
}