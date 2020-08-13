using Atlas.MatchPrediction.Test.Verification.Models;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchPrediction.Test.Verification.Validators
{
    internal class GenerateTestHarnessRequestValidator : AbstractValidator<GenerateTestHarnessRequest>
    {
        public GenerateTestHarnessRequestValidator()
        {
            RuleFor(x => x.PatientMaskingRequests).SetValidator(new MaskingRequestsTransferValidator());
            RuleFor(x => x.DonorMaskingRequests).SetValidator(new MaskingRequestsTransferValidator());
        }
    }

    internal class MaskingRequestsTransferValidator : AbstractValidator<MaskingRequestsTransfer>
    {
        public MaskingRequestsTransferValidator()
        {
            RuleFor(x => x.A).SetValidator(new RequiredLocusMaskingRequestCollectionValidator());
            RuleFor(x => x.B).SetValidator(new RequiredLocusMaskingRequestCollectionValidator());
            RuleFor(x => x.Drb1).SetValidator(new RequiredLocusMaskingRequestCollectionValidator());

            RuleFor(x => x.C).SetValidator(new MaskingRequestCollectionValidator());
            RuleFor(x => x.Dqb1).SetValidator(new MaskingRequestCollectionValidator());
        }
    }

    internal class RequiredLocusMaskingRequestCollectionValidator : MaskingRequestCollectionValidator
    {
        public RequiredLocusMaskingRequestCollectionValidator()
        {
            RuleForEach(x => x.Select(m => m.MaskingCategory))
                .NotEqual(MaskingCategory.Delete);
        }
    }

    internal class MaskingRequestCollectionValidator : AbstractValidator<IEnumerable<MaskingRequest>>
    {
        public MaskingRequestCollectionValidator()
        {
            RuleForEach(x => x).SetValidator(new MaskingRequestValidator());

            RuleFor(x => x.Sum(m => m.ProportionToMask))
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(100);
        }
    }

    internal class MaskingRequestValidator : AbstractValidator<MaskingRequest>
    {
        public MaskingRequestValidator()
        {
            RuleFor(x => x.ProportionToMask).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
            RuleFor(x => x.MaskingCategory).IsInEnum();
        }
    }
}