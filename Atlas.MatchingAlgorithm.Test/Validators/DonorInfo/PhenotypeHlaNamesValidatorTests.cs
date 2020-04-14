using FluentValidation.TestHelper;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using Nova.Utils.PhenotypeInfo;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.DonorInfo
{
    [TestFixture]
    public class PhenotypeHlaNamesValidatorTests
    {
        private PhenotypeHlaNamesValidator validator;
        
        [SetUp]
        public void SetUp()
        {
            validator = new PhenotypeHlaNamesValidator();
        }
        
        [Test]
        public void Validator_WhenMissingLocusA_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.A, (LocusInfo<string>) null);
        }

        [Test]
        public void Validator_WhenMissingLocusB_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.B, (LocusInfo<string>) null);
        }

        [Test]
        public void Validator_WhenMissingLocusDrb1_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.Drb1, (LocusInfo<string>) null);
        }

        [Test]
        public void Validator_WhenMissingLocusC_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.C, (LocusInfo<string>) null);
        }

        [Test]
        public void Validator_WhenMissingLocusDqb1_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.Dqb1, (LocusInfo<string>)null);
        }

        [Test]
        public void Validator_WhenMissingLocusDpb1_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.Dpb1, (LocusInfo<string>)null);
        }
    }
}