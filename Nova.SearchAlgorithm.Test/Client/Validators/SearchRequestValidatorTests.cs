using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.TestHelper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Client.Validators
{
    [TestFixture]
    public class SearchRequestsValidatorTests
    {
        private SearchRequestValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new SearchRequestValidator();
        }

        [Test]
        public void Validator_WhenMatchCriteriaMissing_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.MatchCriteria, (MismatchCriteria) null);
        }

        [Test]
        public void Validator_WhenMatchRegistriesMissing_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.RegistriesToSearch, (IEnumerable<RegistryCode>) null);
        }

        [Test]
        public void Validator_WhenAnyRegistryInvalid_ShouldHaveValidationError()
        {
            var result = validator.Validate(new SearchRequest
            {
                SearchType = DonorType.Adult,
                MatchCriteria = new MismatchCriteria(),
                RegistriesToSearch = new[] { RegistryCode.AN, (RegistryCode) 999 }
            });
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenSearchTypeMissing_ShouldHaveValidationError()
        {
            var result = validator.Validate(new SearchRequest
            {
                MatchCriteria = new MismatchCriteria(),
                RegistriesToSearch = new []{ RegistryCode.AN }
            });
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WithInvalidSearchType_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.SearchType, (DonorType) 999);
        }

        [Test]
        public void Validator_WithMatchCriteriaForLocusCAndNoHlaDataAtC_ShouldHaveValidationError()
        {
            var result = validator.Validate(new SearchRequest
            {
                RegistriesToSearch = new []{ RegistryCode.AN },
                SearchType = DonorType.Adult,
                MatchCriteria = new MismatchCriteria
                {
                    LocusMismatchA = new LocusMismatchCriteria(),
                    LocusMismatchB = new LocusMismatchCriteria(),
                    LocusMismatchDrb1 = new LocusMismatchCriteria(),
                    LocusMismatchC = new LocusMismatchCriteria(),
                },
                SearchHlaData = new SearchHlaData
                {
                    LocusSearchHlaA = new LocusSearchHla{ SearchHla1 = "hla", SearchHla2 = "hla"},
                    LocusSearchHlaB = new LocusSearchHla{ SearchHla1 = "hla", SearchHla2 = "hla"},
                    LocusSearchHlaDrb1 = new LocusSearchHla{ SearchHla1 = "hla", SearchHla2 = "hla"},
                }
            });
            result.IsValid.Should().BeFalse();
        }
        
        [Test]
        public void Validator_WithMatchCriteriaForLocusDqb1AndNoHlaDataAtDqb1_ShouldHaveValidationError()
        {
            var result = validator.Validate(new SearchRequest
            {
                RegistriesToSearch = new []{ RegistryCode.AN },
                SearchType = DonorType.Adult,
                MatchCriteria = new MismatchCriteria
                {
                    LocusMismatchA = new LocusMismatchCriteria(),
                    LocusMismatchB = new LocusMismatchCriteria(),
                    LocusMismatchDrb1 = new LocusMismatchCriteria(),
                    LocusMismatchDqb1 = new LocusMismatchCriteria(),
                },
                SearchHlaData = new SearchHlaData
                {
                    LocusSearchHlaA = new LocusSearchHla{ SearchHla1 = "hla", SearchHla2 = "hla"},
                    LocusSearchHlaB = new LocusSearchHla{ SearchHla1 = "hla", SearchHla2 = "hla"},
                    LocusSearchHlaDrb1 = new LocusSearchHla{ SearchHla1 = "hla", SearchHla2 = "hla"},
                }
            });
            result.IsValid.Should().BeFalse();
        }
    }
}
