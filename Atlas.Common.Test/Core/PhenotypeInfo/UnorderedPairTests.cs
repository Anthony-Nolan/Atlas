using Atlas.Common.GeneticData;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    public class UnorderedPairTests
    {
        [Test]
        public void CheckEquality_WhenDiplotypesHaveSameHaplotypesInSameOrder_ShouldBeTrue()
        {
            const string testData1 = "Test data 1";
            const string testData2 = "Test data 2";

            var unorderedPair1 = new UnorderedPair<string, string>()
            {
                Item1 = testData1,
                Item2 = testData2
            };

            var unorderedPair2 = new UnorderedPair<string, string>()
            {
                Item1 = testData1,
                Item2 = testData2
            };

            unorderedPair1.Should().BeEquivalentTo(unorderedPair2);
            unorderedPair1.GetHashCode().Should().Be(unorderedPair2.GetHashCode());
        }

        [Test]
        public void CheckEquality_WhenDiplotypesHaveSameHaplotypesSwapped_ShouldBeTrue()
        {
            const string testData1 = "Test data 1";
            const string testData2 = "Test data 2";

            var unorderedPair1 = new UnorderedPair<string, string>()
            {
                Item1 = testData1,
                Item2 = testData2
            };

            var unorderedPair2 = new UnorderedPair<string, string>()
            {
                Item1 = testData2,
                Item2 = testData1
            };

            unorderedPair1.Should().BeEquivalentTo(unorderedPair2);
            unorderedPair1.GetHashCode().Should().Be(unorderedPair2.GetHashCode());
        }

        [Test]
        public void CheckEquality_WhenDiplotypesHaveDifferentHaplotypes_ShouldBeFalse()
        {
            const string testData1 = "Test data 1";
            const string testData2 = "Test data 2";

            const string differentTestData1 = "Test data 1";
            const string differentTestData2 = "Test data 2";

            var unorderedPair1 = new UnorderedPair<string, string>()
            {
                Item1 = testData1,
                Item2 = testData2
            };

            var unorderedPair2 = new UnorderedPair<string, string>()
            {
                Item1 = differentTestData1,
                Item2 = differentTestData2
            };

            unorderedPair1.Should().BeEquivalentTo(unorderedPair2);
            unorderedPair1.GetHashCode().Should().Be(unorderedPair2.GetHashCode());
        }
    }
}
