using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Matching
{
    [TestFixture]
    public class DatabaseFilteringAnalyserTests
    {
        private IDatabaseFilteringAnalyser databaseFilteringAnalyser;

        [SetUp]
        public void SetUp()
        {
            databaseFilteringAnalyser = new DatabaseFilteringAnalyser();
        }

        [Test]
        public void ShouldFilterOnDonorTypeInDatabase_ForAdult_ReturnsFalse()
        {
            var criteria = new LocusSearchCriteria
            {
                SearchDonorType = DonorType.Adult
            };

            var result = databaseFilteringAnalyser.ShouldFilterOnDonorTypeInDatabase(criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void ShouldFilterOnDonorTypeInDatabase_ForCord_ReturnsTrue()
        {
            var criteria = new LocusSearchCriteria
            {
                SearchDonorType = DonorType.Cord
            };

            var result = databaseFilteringAnalyser.ShouldFilterOnDonorTypeInDatabase(criteria);

            result.Should().BeTrue();
        }
    }
}