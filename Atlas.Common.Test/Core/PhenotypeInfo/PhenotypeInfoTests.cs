using FluentAssertions;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    [TestFixture]
    public class PhenotypeInfoTests
    {
        [Test]
        public void GetAtPosition_GetsValuesAtSpecifiedPositionForAllLoci()
        {
            var phenotypeInfo = new PhenotypeInfo<string>();

            const string testDataA1 = "[TEST] At locus A, position 1";
            phenotypeInfo.A.Position1 = testDataA1;
            phenotypeInfo.GetPosition(Locus.A, LocusPosition.Position1).Should().Be(testDataA1);

            const string testDataA2 = "[TEST] At locus A, position 2";
            phenotypeInfo.A.Position2 = testDataA2;
            phenotypeInfo.GetPosition(Locus.A, LocusPosition.Position2).Should().Be(testDataA2);

            const string testDataB1 = "[TEST] At locus B, position 1";
            phenotypeInfo.B.Position1 = testDataB1;
            phenotypeInfo.GetPosition(Locus.B, LocusPosition.Position1).Should().Be(testDataB1);

            const string testDataB2 = "[TEST] At locus B, position 2";
            phenotypeInfo.B.Position2 = testDataB2;
            phenotypeInfo.GetPosition(Locus.B, LocusPosition.Position2).Should().Be(testDataB2);

            const string testDataC1 = "[TEST] At locus C, position 1";
            phenotypeInfo.C.Position1 = testDataC1;
            phenotypeInfo.GetPosition(Locus.C, LocusPosition.Position1).Should().Be(testDataC1);

            const string testDataC2 = "[TEST] At locus C, position 2";
            phenotypeInfo.C.Position2 = testDataC2;
            phenotypeInfo.GetPosition(Locus.C, LocusPosition.Position2).Should().Be(testDataC2);

            const string testDataDrb11 = "[TEST] At locus Drb1, position 1";
            phenotypeInfo.Drb1.Position1 = testDataDrb11;
            phenotypeInfo.GetPosition(Locus.Drb1, LocusPosition.Position1).Should().Be(testDataDrb11);

            const string testDataDrb12 = "[TEST] At locus Drb1, position 2";
            phenotypeInfo.Drb1.Position2 = testDataDrb12;
            phenotypeInfo.GetPosition(Locus.Drb1, LocusPosition.Position2).Should().Be(testDataDrb12);

            const string testDataDqb11 = "[TEST] At locus Dqb1, position 1";
            phenotypeInfo.Dqb1.Position1 = testDataDqb11;
            phenotypeInfo.GetPosition(Locus.Dqb1, LocusPosition.Position1).Should().Be(testDataDqb11);

            const string testDataDqb12 = "[TEST] At locus Dqb1, position 2";
            phenotypeInfo.Dqb1.Position2 = testDataDqb12;
            phenotypeInfo.GetPosition(Locus.Dqb1, LocusPosition.Position2).Should().Be(testDataDqb12);

            const string testDataDpb11 = "[TEST] At locus Dpb1, position 1";
            phenotypeInfo.Dpb1.Position1 = testDataDpb11;
            phenotypeInfo.GetPosition(Locus.Dpb1, LocusPosition.Position1).Should().Be(testDataDpb11);

            const string testDataDpb12 = "[TEST] At locus Dpb1, position 2";
            phenotypeInfo.Dpb1.Position2 = testDataDpb12;
            phenotypeInfo.GetPosition(Locus.Dpb1, LocusPosition.Position2).Should().Be(testDataDpb12);
        }

        [Test]
        public void SetAtPosition_SetsValuesAtSpecifiedPositionForAllLoci()
        {
            var phenotypeInfo = new PhenotypeInfo<string>();

            const string testDataA1 = "[TEST] At locus A, position 1";
            phenotypeInfo.SetPosition(Locus.A, LocusPosition.Position1, testDataA1);
            phenotypeInfo.A.Position1.Should().Be(testDataA1);


            const string testDataA2 = "[TEST] At locus A, position 2";
            phenotypeInfo.SetPosition(Locus.A, LocusPosition.Position2, testDataA2);
            phenotypeInfo.A.Position2.Should().Be(testDataA2);


            const string testDataB1 = "[TEST] At locus B, position 1";
            phenotypeInfo.SetPosition(Locus.B, LocusPosition.Position1, testDataB1);
            phenotypeInfo.B.Position1.Should().Be(testDataB1);


            const string testDataB2 = "[TEST] At locus B, position 2";
            phenotypeInfo.SetPosition(Locus.B, LocusPosition.Position2, testDataB2);
            phenotypeInfo.B.Position2.Should().Be(testDataB2);


            const string testDataC1 = "[TEST] At locus C, position 1";
            phenotypeInfo.SetPosition(Locus.C, LocusPosition.Position1, testDataC1);
            phenotypeInfo.C.Position1.Should().Be(testDataC1);


            const string testDataC2 = "[TEST] At locus C, position 2";
            phenotypeInfo.SetPosition(Locus.C, LocusPosition.Position2, testDataC2);
            phenotypeInfo.C.Position2.Should().Be(testDataC2);


            const string testDataDrb11 = "[TEST] At locus Drb1, position 1";
            phenotypeInfo.SetPosition(Locus.Drb1, LocusPosition.Position1, testDataDrb11);
            phenotypeInfo.Drb1.Position1.Should().Be(testDataDrb11);


            const string testDataDrb12 = "[TEST] At locus Drb1, position 2";
            phenotypeInfo.SetPosition(Locus.Drb1, LocusPosition.Position2, testDataDrb12);
            phenotypeInfo.Drb1.Position2.Should().Be(testDataDrb12);

            const string testDataDqb11 = "[TEST] At locus Dqb1, position 1";
            phenotypeInfo.SetPosition(Locus.Dqb1, LocusPosition.Position1, testDataDqb11);
            phenotypeInfo.Dqb1.Position1.Should().Be(testDataDqb11);


            const string testDataDqb12 = "[TEST] At locus Dqb1, position 2";
            phenotypeInfo.SetPosition(Locus.Dqb1, LocusPosition.Position2, testDataDqb12);
            phenotypeInfo.Dqb1.Position2.Should().Be(testDataDqb12);


            const string testDataDpb11 = "[TEST] At locus Dpb1, position 1";
            phenotypeInfo.SetPosition(Locus.Dpb1, LocusPosition.Position1, testDataDpb11);
            phenotypeInfo.Dpb1.Position1.Should().Be(testDataDpb11);


            const string testDataDpb12 = "[TEST] At locus Dpb1, position 2";
            phenotypeInfo.SetPosition(Locus.Dpb1, LocusPosition.Position2, testDataDpb12);
            phenotypeInfo.Dpb1.Position2.Should().Be(testDataDpb12);
        }
    }
}