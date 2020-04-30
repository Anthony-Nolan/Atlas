using FluentAssertions;
using Atlas.Utils.Core.Models;
using Atlas.Utils.Core.PhenotypeInfo;
using NUnit.Framework;

namespace Atlas.Utils.Test.Core.PhenotypeInfo
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
            phenotypeInfo.GetAtPosition(LocusType.A, LocusPosition.Position1).Should().Be(testDataA1);

            const string testDataA2 = "[TEST] At locus A, position 2";
            phenotypeInfo.A.Position2 = testDataA2;
            phenotypeInfo.GetAtPosition(LocusType.A, LocusPosition.Position2).Should().Be(testDataA2);

            const string testDataB1 = "[TEST] At locus B, position 1";
            phenotypeInfo.B.Position1 = testDataB1;
            phenotypeInfo.GetAtPosition(LocusType.B, LocusPosition.Position1).Should().Be(testDataB1);

            const string testDataB2 = "[TEST] At locus B, position 2";
            phenotypeInfo.B.Position2 = testDataB2;
            phenotypeInfo.GetAtPosition(LocusType.B, LocusPosition.Position2).Should().Be(testDataB2);

            const string testDataC1 = "[TEST] At locus C, position 1";
            phenotypeInfo.C.Position1 = testDataC1;
            phenotypeInfo.GetAtPosition(LocusType.C, LocusPosition.Position1).Should().Be(testDataC1);

            const string testDataC2 = "[TEST] At locus C, position 2";
            phenotypeInfo.C.Position2 = testDataC2;
            phenotypeInfo.GetAtPosition(LocusType.C, LocusPosition.Position2).Should().Be(testDataC2);

            const string testDataDrb11 = "[TEST] At locus Drb1, position 1";
            phenotypeInfo.Drb1.Position1 = testDataDrb11;
            phenotypeInfo.GetAtPosition(LocusType.Drb1, LocusPosition.Position1).Should().Be(testDataDrb11);

            const string testDataDrb12 = "[TEST] At locus Drb1, position 2";
            phenotypeInfo.Drb1.Position2 = testDataDrb12;
            phenotypeInfo.GetAtPosition(LocusType.Drb1, LocusPosition.Position2).Should().Be(testDataDrb12);

            const string testDataDrb31 = "[TEST] At locus Drb3, position 1";
            phenotypeInfo.Drb3.Position1 = testDataDrb31;
            phenotypeInfo.GetAtPosition(LocusType.Drb3, LocusPosition.Position1).Should().Be(testDataDrb31);

            const string testDataDrb32 = "[TEST] At locus Drb3, position 2";
            phenotypeInfo.Drb3.Position2 = testDataDrb32;
            phenotypeInfo.GetAtPosition(LocusType.Drb3, LocusPosition.Position2).Should().Be(testDataDrb32);

            const string testDataDrb41 = "[TEST] At locus Drb4, position 1";
            phenotypeInfo.Drb4.Position1 = testDataDrb41;
            phenotypeInfo.GetAtPosition(LocusType.Drb4, LocusPosition.Position1).Should().Be(testDataDrb41);

            const string testDataDrb42 = "[TEST] At locus Drb4, position 2";
            phenotypeInfo.Drb4.Position2 = testDataDrb42;
            phenotypeInfo.GetAtPosition(LocusType.Drb4, LocusPosition.Position2).Should().Be(testDataDrb42);

            const string testDataDrb51 = "[TEST] At locus Drb5, position 1";
            phenotypeInfo.Drb5.Position1 = testDataDrb51;
            phenotypeInfo.GetAtPosition(LocusType.Drb5, LocusPosition.Position1).Should().Be(testDataDrb51);

            const string testDataDrb52 = "[TEST] At locus Drb5, position 2";
            phenotypeInfo.Drb5.Position2 = testDataDrb52;
            phenotypeInfo.GetAtPosition(LocusType.Drb5, LocusPosition.Position2).Should().Be(testDataDrb52);

            const string testDataDqa11 = "[TEST] At locus Dqa1, position 1";
            phenotypeInfo.Dqa1.Position1 = testDataDqa11;
            phenotypeInfo.GetAtPosition(LocusType.Dqa1, LocusPosition.Position1).Should().Be(testDataDqa11);

            const string testDataDqa12 = "[TEST] At locus Dqa1, position 2";
            phenotypeInfo.Dqa1.Position2 = testDataDqa12;
            phenotypeInfo.GetAtPosition(LocusType.Dqa1, LocusPosition.Position2).Should().Be(testDataDqa12);

            const string testDataDqb11 = "[TEST] At locus Dqb1, position 1";
            phenotypeInfo.Dqb1.Position1 = testDataDqb11;
            phenotypeInfo.GetAtPosition(LocusType.Dqb1, LocusPosition.Position1).Should().Be(testDataDqb11);

            const string testDataDqb12 = "[TEST] At locus Dqb1, position 2";
            phenotypeInfo.Dqb1.Position2 = testDataDqb12;
            phenotypeInfo.GetAtPosition(LocusType.Dqb1, LocusPosition.Position2).Should().Be(testDataDqb12);

            const string testDataDpa11 = "[TEST] At locus Dpa1, position 1";
            phenotypeInfo.Dpa1.Position1 = testDataDpa11;
            phenotypeInfo.GetAtPosition(LocusType.Dpa1, LocusPosition.Position1).Should().Be(testDataDpa11);

            const string testDataDpa12 = "[TEST] At locus Dpa1, position 2";
            phenotypeInfo.Dpa1.Position2 = testDataDpa12;
            phenotypeInfo.GetAtPosition(LocusType.Dpa1, LocusPosition.Position2).Should().Be(testDataDpa12);

            const string testDataDpb11 = "[TEST] At locus Dpb1, position 1";
            phenotypeInfo.Dpb1.Position1 = testDataDpb11;
            phenotypeInfo.GetAtPosition(LocusType.Dpb1, LocusPosition.Position1).Should().Be(testDataDpb11);

            const string testDataDpb12 = "[TEST] At locus Dpb1, position 2";
            phenotypeInfo.Dpb1.Position2 = testDataDpb12;
            phenotypeInfo.GetAtPosition(LocusType.Dpb1, LocusPosition.Position2).Should().Be(testDataDpb12);
        }

        [Test]
        public void SetAtPosition_SetsValuesAtSpecifiedPositionForAllLoci()
        {
            var phenotypeInfo = new PhenotypeInfo<string>();

            const string testDataA1 = "[TEST] At locus A, position 1";
            phenotypeInfo.SetAtPosition(LocusType.A, LocusPosition.Position1, testDataA1);
            phenotypeInfo.A.Position1.Should().Be(testDataA1);


            const string testDataA2 = "[TEST] At locus A, position 2";
            phenotypeInfo.SetAtPosition(LocusType.A, LocusPosition.Position2, testDataA2);
            phenotypeInfo.A.Position2.Should().Be(testDataA2);


            const string testDataB1 = "[TEST] At locus B, position 1";
            phenotypeInfo.SetAtPosition(LocusType.B, LocusPosition.Position1, testDataB1);
            phenotypeInfo.B.Position1.Should().Be(testDataB1);


            const string testDataB2 = "[TEST] At locus B, position 2";
            phenotypeInfo.SetAtPosition(LocusType.B, LocusPosition.Position2, testDataB2);
            phenotypeInfo.B.Position2.Should().Be(testDataB2);


            const string testDataC1 = "[TEST] At locus C, position 1";
            phenotypeInfo.SetAtPosition(LocusType.C, LocusPosition.Position1, testDataC1);
            phenotypeInfo.C.Position1.Should().Be(testDataC1);


            const string testDataC2 = "[TEST] At locus C, position 2";
            phenotypeInfo.SetAtPosition(LocusType.C, LocusPosition.Position2, testDataC2);
            phenotypeInfo.C.Position2.Should().Be(testDataC2);


            const string testDataDrb11 = "[TEST] At locus Drb1, position 1";
            phenotypeInfo.SetAtPosition(LocusType.Drb1, LocusPosition.Position1, testDataDrb11);
            phenotypeInfo.Drb1.Position1.Should().Be(testDataDrb11);


            const string testDataDrb12 = "[TEST] At locus Drb1, position 2";
            phenotypeInfo.SetAtPosition(LocusType.Drb1, LocusPosition.Position2, testDataDrb12);
            phenotypeInfo.Drb1.Position2.Should().Be(testDataDrb12);


            const string testDataDrb31 = "[TEST] At locus Drb3, position 1";
            phenotypeInfo.SetAtPosition(LocusType.Drb3, LocusPosition.Position1, testDataDrb31);
            phenotypeInfo.Drb3.Position1.Should().Be(testDataDrb31);


            const string testDataDrb32 = "[TEST] At locus Drb3, position 2";
            phenotypeInfo.SetAtPosition(LocusType.Drb3, LocusPosition.Position2, testDataDrb32);
            phenotypeInfo.Drb3.Position2.Should().Be(testDataDrb32);


            const string testDataDrb41 = "[TEST] At locus Drb4, position 1";
            phenotypeInfo.SetAtPosition(LocusType.Drb4, LocusPosition.Position1, testDataDrb41);
            phenotypeInfo.Drb4.Position1.Should().Be(testDataDrb41);


            const string testDataDrb42 = "[TEST] At locus Drb4, position 2";
            phenotypeInfo.SetAtPosition(LocusType.Drb4, LocusPosition.Position2, testDataDrb42);
            phenotypeInfo.Drb4.Position2.Should().Be(testDataDrb42);


            const string testDataDrb51 = "[TEST] At locus Drb5, position 1";
            phenotypeInfo.SetAtPosition(LocusType.Drb5, LocusPosition.Position1, testDataDrb51);
            phenotypeInfo.Drb5.Position1.Should().Be(testDataDrb51);


            const string testDataDrb52 = "[TEST] At locus Drb5, position 2";
            phenotypeInfo.SetAtPosition(LocusType.Drb5, LocusPosition.Position2, testDataDrb52);
            phenotypeInfo.Drb5.Position2.Should().Be(testDataDrb52);


            const string testDataDqa11 = "[TEST] At locus Dqa1, position 1";
            phenotypeInfo.SetAtPosition(LocusType.Dqa1, LocusPosition.Position1, testDataDqa11);
            phenotypeInfo.Dqa1.Position1.Should().Be(testDataDqa11);


            const string testDataDqa12 = "[TEST] At locus Dqa1, position 2";
            phenotypeInfo.SetAtPosition(LocusType.Dqa1, LocusPosition.Position2, testDataDqa12);
            phenotypeInfo.Dqa1.Position2.Should().Be(testDataDqa12);


            const string testDataDqb11 = "[TEST] At locus Dqb1, position 1";
            phenotypeInfo.SetAtPosition(LocusType.Dqb1, LocusPosition.Position1, testDataDqb11);
            phenotypeInfo.Dqb1.Position1.Should().Be(testDataDqb11);


            const string testDataDqb12 = "[TEST] At locus Dqb1, position 2";
            phenotypeInfo.SetAtPosition(LocusType.Dqb1, LocusPosition.Position2, testDataDqb12);
            phenotypeInfo.Dqb1.Position2.Should().Be(testDataDqb12);


            const string testDataDpa11 = "[TEST] At locus Dpa1, position 1";
            phenotypeInfo.SetAtPosition(LocusType.Dpa1, LocusPosition.Position1, testDataDpa11);
            phenotypeInfo.Dpa1.Position1.Should().Be(testDataDpa11);


            const string testDataDpa12 = "[TEST] At locus Dpa1, position 2";
            phenotypeInfo.SetAtPosition(LocusType.Dpa1, LocusPosition.Position2, testDataDpa12);
            phenotypeInfo.Dpa1.Position2.Should().Be(testDataDpa12);


            const string testDataDpb11 = "[TEST] At locus Dpb1, position 1";
            phenotypeInfo.SetAtPosition(LocusType.Dpb1, LocusPosition.Position1, testDataDpb11);
            phenotypeInfo.Dpb1.Position1.Should().Be(testDataDpb11);


            const string testDataDpb12 = "[TEST] At locus Dpb1, position 2";
            phenotypeInfo.SetAtPosition(LocusType.Dpb1, LocusPosition.Position2, testDataDpb12);
            phenotypeInfo.Dpb1.Position2.Should().Be(testDataDpb12);
        }
    }
}