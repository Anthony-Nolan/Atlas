using Atlas.Common.FeatureManagement;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.Models.Mapping;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.Donors;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorHelperTests
    {
        private IMatchingAlgorithmSearchLogger searchLogger;
        private IDonorReader donorReader;
        private IAtlasFeatureManager featureManager;

        [SetUp]
        public void SetUp()
        {
            searchLogger = Substitute.For<IMatchingAlgorithmSearchLogger>();

            donorReader = Substitute.For<IDonorReader>();
            donorReader.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new List<Donor> { new Donor { ExternalDonorCode = "donor_from_donor_store" } }.ToDictionary(_ => 1, v => v.ToPublicDonor()));

            featureManager = Substitute.For<IAtlasFeatureManager>();
        }

        [Test]
        public async Task GetDonorLookup_FeatureFlagDisabled_GetsDataFromDonorStore()
        {
            // Arrange
            featureManager.IsFeatureEnabled(Arg.Any<string>()).Returns(false);

            var donorHelper = new DonorHelper(searchLogger, donorReader, featureManager);

            var reifiedScoredMatches = new List<MatchAndScoreResult>
            {
                new ()
                {
                    MatchResult = new (1)
                    {
                        DonorInfo = new ()
                        {
                            ExternalDonorCode = "donor_from_matching_result"
                        }
                    }
                }
            };

            // Act
            var actual = await donorHelper.GetDonorLookup(reifiedScoredMatches);

            // Assert
            actual.Should().NotBeNull();
            var donor = actual.FirstOrDefault();
            donor.Should().NotBeNull();
            donor.Value.ExternalDonorCode.Should().Be("donor_from_donor_store");

            await donorReader.Received().GetDonors(Arg.Any<IEnumerable<int>>());
        }

        [Test]
        public async Task GetDonorLookup_FeatureFlagEnabled_GetsDataFromMatchingResultObject()
        {
            // Arrange
            featureManager.IsFeatureEnabled(Arg.Any<string>()).Returns(true);

            var donorHelper = new DonorHelper(searchLogger, donorReader, featureManager);

            var reifiedScoredMatches = new List<MatchAndScoreResult>
            {
                new ()
                {
                    MatchResult = new (1)
                    {
                        DonorInfo = new ()
                        {
                            ExternalDonorCode = "donor_from_matching_result"
                        }
                    }
                }
            };

            // Act
            var actual = await donorHelper.GetDonorLookup(reifiedScoredMatches);

            // Assert
            actual.Should().NotBeNull();
            var donor = actual.FirstOrDefault();
            donor.Should().NotBeNull();
            donor.Value.ExternalDonorCode.Should().Be("donor_from_matching_result");

            await donorReader.DidNotReceive().GetDonors(Arg.Any<IEnumerable<int>>());
        }
    }
}
