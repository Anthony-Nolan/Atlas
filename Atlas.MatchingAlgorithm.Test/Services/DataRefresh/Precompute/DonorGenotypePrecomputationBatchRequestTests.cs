using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using AutoFixture;
using AwesomeAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

/// <summary>
/// The body of the batch message. The worker and the dead-letter trigger read the batch from it.
/// </summary>
[TestFixture]
public class DonorGenotypePrecomputationBatchRequestTests
{
    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public void FromBody_OfTheJsonOfARequest_ReturnsAnEqualRequest()
    {
        var request = fixture.Create<DonorGenotypePrecomputationBatchRequest>();

        var result = DonorGenotypePrecomputationBatchRequest.FromBody(JsonConvert.SerializeObject(request));

        result.Should().BeEquivalentTo(request);
    }

    [Test]
    public void FromBody_WhenTheBodyIsNotJson_ReturnsNull()
    {
        DonorGenotypePrecomputationBatchRequest.FromBody(fixture.Create<string>()).Should().BeNull();
    }

    [TestCase("{}")]
    [TestCase("")]
    public void FromBody_WhenTheBodyNamesNoBatch_ReturnsNull(string body)
    {
        // An empty object reads as a batch with the ids 0, which no batch has.
        DonorGenotypePrecomputationBatchRequest.FromBody(body).Should().BeNull();
    }
}
