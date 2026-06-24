using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.SearchRequest;

[TestFixture]
public class SearchRequestValidatorTests
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
        var request = new Atlas.Client.Models.Search.Requests.SearchRequest { MatchCriteria = null };
        validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.MatchCriteria);
    }

    [Test]
    public void Validator_WithInvalidSearchType_ShouldHaveValidationError()
    {
        var request = new Atlas.Client.Models.Search.Requests.SearchRequest { SearchDonorType = (Atlas.Client.Models.Search.DonorType) 999 };
        validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.SearchDonorType);
    }

    [TestCase(Locus.C)]
    [TestCase(Locus.Dqb1)]
    public void Validator_WithMatchCriteriaForOptionalLocus_ButNoHlaDataAtThatLocus_ShouldHaveValidationError(Locus locus)
    {
        var matchingRequest = new SearchRequestBuilder()
            .WithLocusMismatchCount(locus, 1)
            .WithNullLocusSearchHla(locus)
            .Build();

        var result = validator.Validate(matchingRequest);
        result.IsValid.Should().BeFalse();
    }
}