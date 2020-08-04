using Atlas.Common.GeneticData;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    public class UnorderedPairTests
    {
        [Test]
        public void CheckEquality_WhenDataPairInSameOrder_ShouldBeTrue()
        {
            const string testData1 = "Test data 1";
            const string testData2 = "Test data 2";

            var unorderedPair1 = new UnorderedPair<string>(testData1, testData2);
            var unorderedPair2 = new UnorderedPair<string>(testData1, testData2);

            unorderedPair1.Should().BeEquivalentTo(unorderedPair2);
            unorderedPair1.GetHashCode().Should().Be(unorderedPair2.GetHashCode());
        }

        [Test]
        public void CheckEquality_WhenDataPairPositionsSwapped_ShouldBeTrue()
        {
            const string testData1 = "Test data 1";
            const string testData2 = "Test data 2";

            var unorderedPair1 = new UnorderedPair<string>(testData1, testData2);
            var unorderedPair2 = new UnorderedPair<string>(testData2, testData1);

            unorderedPair1.Should().BeEquivalentTo(unorderedPair2);
            unorderedPair1.GetHashCode().Should().Be(unorderedPair2.GetHashCode());
        }

        [Test]
        public void CheckEquality_WhenDataPairValuesAreDifferent_ShouldBeFalse()
        {
            const string testData1 = "Test data 1";
            const string testData2 = "Test data 2";

            const string differentTestData1 = "Different test data 1";
            const string differentTestData2 = "Different test data 2";

            var unorderedPair1 = new UnorderedPair<string>(testData1, testData2);
            var unorderedPair2 = new UnorderedPair<string>(differentTestData1, differentTestData2);

            unorderedPair1.Should().NotBeEquivalentTo(unorderedPair2);
            unorderedPair1.GetHashCode().Should().NotBe(unorderedPair2.GetHashCode());
        }
    }
}