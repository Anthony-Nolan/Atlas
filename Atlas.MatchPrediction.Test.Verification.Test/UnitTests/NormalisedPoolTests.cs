using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class NormalisedPoolTests
    {
        [Test]
        public void New_CalculatesCorrectTotalCopyNumber()
        {
            const int firstCopyNumber = 4;
            const int secondCopyNumber = 12;

            var firstMember = NormalisedPoolMemberBuilder.New
                .With(x => x.CopyNumber, firstCopyNumber)
                .Build();

            var secondMember = NormalisedPoolMemberBuilder.New
                .With(x => x.CopyNumber, secondCopyNumber)
                .Build();

            var pool = new NormalisedHaplotypePool(default, default, new[] { firstMember, secondMember });

            pool.TotalCopyNumber.Should().Be(firstCopyNumber + secondCopyNumber);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        [TestCase(5, false)]
        public void GetHaplotypeFrequencyByPoolIndex_ReturnsCorrectHaplotype(int poolIndex, bool isSecondMember)
        {
            const string secondMemberA = "2nd-member-A";

            var firstMember = NormalisedPoolMemberBuilder.New
                .With(x => x.CopyNumber, 2)
                .With(x => x.PoolIndexLowerBoundary, 0)
                .Build();

            var secondMember = NormalisedPoolMemberBuilder.New
                .With(x => x.HaplotypeFrequency, HaplotypeFrequencyBuilder.Default.With(h => h.A, secondMemberA))
                .With(x => x.CopyNumber, 3)
                .With(x => x.PoolIndexLowerBoundary, firstMember.PoolIndexUpperBoundary + 1)
                .Build();

            var thirdMember = NormalisedPoolMemberBuilder.New
                .With(x => x.CopyNumber, 1)
                .With(x => x.PoolIndexLowerBoundary, secondMember.PoolIndexUpperBoundary + 1)
                .Build();

            var pool = new NormalisedHaplotypePool(default, default, new [] { firstMember, secondMember, thirdMember });

            var result = pool.GetHaplotypeFrequencyByPoolIndex(poolIndex);

            result.A.Equals(secondMemberA).Should().Be(isSecondMember);
        }
    }
}
