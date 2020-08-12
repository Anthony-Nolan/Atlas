using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.Validation;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using FluentValidation;

namespace Atlas.MatchPrediction.Validators
{
    internal class MatchProbabilityInputValidator : AbstractValidator<SingleDonorMatchProbabilityInput>
    {
        private readonly List<Locus> requiredLoci = new List<Locus> {Locus.A, Locus.B, Locus.Drb1};
        
        public MatchProbabilityInputValidator()
        {
            RuleFor(i => i.DonorInput).NotNull().SetValidator(new MatchProbabilityDonorInputValidator());
            RuleFor(i => i.PatientHla).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
            RuleFor(i => i.HlaNomenclatureVersion).NotEmpty();
            RuleForEach(i => i.ExcludedLoci).Must(l => !requiredLoci.Contains(l));
        }
    }

    internal class MatchProbabilityDonorInputValidator : AbstractValidator<DonorInput>
    {
        public MatchProbabilityDonorInputValidator()
        {
            RuleFor(i => i.DonorHla).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
        }
    }

    public class MatchProbabilityNonDonorValidator : AbstractValidator<MatchProbabilityRequestInput>
    {
        private readonly List<Locus> requiredLoci = new List<Locus> {Locus.A, Locus.B, Locus.Drb1};
        public MatchProbabilityNonDonorValidator()
        {
            RuleFor(i => i.PatientHla).NotNull().SetValidator(new PhenotypeHlaNamesValidator());
            RuleForEach(i => i.ExcludedLoci).Must(l => !requiredLoci.Contains(l));
        }
        
    }
}