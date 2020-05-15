using FluentAssertions;
using Atlas.Common.GeneticData.PhenotypeInfo;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    [TestFixture]
    public class LocusInfoTests
    {
        [Test]
        public void GetAtPosition_GetsValueAtSpecifiedPosition()
        {
            const string position1 = "[TEST] FIRST POSITION";
            const string position2 = "[TEST] SECOND POSITION";
            var locusInfo = new LocusInfo<string>()
            {
                Position1 = position1,
                Position2 = position2
            };

            locusInfo.GetAtPosition(LocusPosition.Position1).Should().Be(position1);
            locusInfo.GetAtPosition(LocusPosition.Position2).Should().Be(position2);
        }

        [Test]
        public void SetAtPosition_SetsValueAtSpecifiedPosition()
        {
            const string position1 = "[TEST] FIRST POSITION";
            const string position2 = "[TEST] SECOND POSITION";
            var locusInfo = new LocusInfo<string>();

            locusInfo.SetAtPosition(LocusPosition.Position1, position1);
            locusInfo.SetAtPosition(LocusPosition.Position2, position2);

            locusInfo.Position1.Should().Be(position1);
            locusInfo.Position2.Should().Be(position2);
        }
    }
}