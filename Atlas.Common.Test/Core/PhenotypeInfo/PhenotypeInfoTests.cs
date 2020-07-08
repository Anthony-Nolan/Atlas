using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    [TestFixture]
    public class PhenotypeInfoTests
    {
        [Test]
        public void Set_LocusProperty_CannotBeNull()
        {
            var phenotypeInfo = new PhenotypeInfo<string>("default");
            phenotypeInfo.Invoking(p => p.A = null).Should().Throw<ArgumentNullException>();
            phenotypeInfo.Invoking(p => p.B = null).Should().Throw<ArgumentNullException>();
            phenotypeInfo.Invoking(p => p.C = null).Should().Throw<ArgumentNullException>();
            phenotypeInfo.Invoking(p => p.Dpb1 = null).Should().Throw<ArgumentNullException>();
            phenotypeInfo.Invoking(p => p.Dqb1 = null).Should().Throw<ArgumentNullException>();
            phenotypeInfo.Invoking(p => p.Drb1 = null).Should().Throw<ArgumentNullException>();
        }
        
        [Test]
        public void GetAtPosition_GetsValuesAtSpecifiedPositionForAllLoci()
        {
            var phenotypeInfo = new PhenotypeInfo<string>();

            const string testDataA1 = "[TEST] At locus A, position 1";
            phenotypeInfo.A.Position1 = testDataA1;
            phenotypeInfo.GetPosition(Locus.A, LocusPosition.One).Should().Be(testDataA1);

            const string testDataA2 = "[TEST] At locus A, position 2";
            phenotypeInfo.A.Position2 = testDataA2;
            phenotypeInfo.GetPosition(Locus.A, LocusPosition.Two).Should().Be(testDataA2);

            const string testDataB1 = "[TEST] At locus B, position 1";
            phenotypeInfo.B.Position1 = testDataB1;
            phenotypeInfo.GetPosition(Locus.B, LocusPosition.One).Should().Be(testDataB1);

            const string testDataB2 = "[TEST] At locus B, position 2";
            phenotypeInfo.B.Position2 = testDataB2;
            phenotypeInfo.GetPosition(Locus.B, LocusPosition.Two).Should().Be(testDataB2);

            const string testDataC1 = "[TEST] At locus C, position 1";
            phenotypeInfo.C.Position1 = testDataC1;
            phenotypeInfo.GetPosition(Locus.C, LocusPosition.One).Should().Be(testDataC1);

            const string testDataC2 = "[TEST] At locus C, position 2";
            phenotypeInfo.C.Position2 = testDataC2;
            phenotypeInfo.GetPosition(Locus.C, LocusPosition.Two).Should().Be(testDataC2);

            const string testDataDrb11 = "[TEST] At locus Drb1, position 1";
            phenotypeInfo.Drb1.Position1 = testDataDrb11;
            phenotypeInfo.GetPosition(Locus.Drb1, LocusPosition.One).Should().Be(testDataDrb11);

            const string testDataDrb12 = "[TEST] At locus Drb1, position 2";
            phenotypeInfo.Drb1.Position2 = testDataDrb12;
            phenotypeInfo.GetPosition(Locus.Drb1, LocusPosition.Two).Should().Be(testDataDrb12);

            const string testDataDqb11 = "[TEST] At locus Dqb1, position 1";
            phenotypeInfo.Dqb1.Position1 = testDataDqb11;
            phenotypeInfo.GetPosition(Locus.Dqb1, LocusPosition.One).Should().Be(testDataDqb11);

            const string testDataDqb12 = "[TEST] At locus Dqb1, position 2";
            phenotypeInfo.Dqb1.Position2 = testDataDqb12;
            phenotypeInfo.GetPosition(Locus.Dqb1, LocusPosition.Two).Should().Be(testDataDqb12);

            const string testDataDpb11 = "[TEST] At locus Dpb1, position 1";
            phenotypeInfo.Dpb1.Position1 = testDataDpb11;
            phenotypeInfo.GetPosition(Locus.Dpb1, LocusPosition.One).Should().Be(testDataDpb11);

            const string testDataDpb12 = "[TEST] At locus Dpb1, position 2";
            phenotypeInfo.Dpb1.Position2 = testDataDpb12;
            phenotypeInfo.GetPosition(Locus.Dpb1, LocusPosition.Two).Should().Be(testDataDpb12);
        }

        [Test]
        public void SetAtPosition_SetsValuesAtSpecifiedPositionForAllLoci()
        {
            var phenotypeInfo = new PhenotypeInfo<string>();

            const string testDataA1 = "[TEST] At locus A, position 1";
            phenotypeInfo.SetPosition(Locus.A, LocusPosition.One, testDataA1);
            phenotypeInfo.A.Position1.Should().Be(testDataA1);


            const string testDataA2 = "[TEST] At locus A, position 2";
            phenotypeInfo.SetPosition(Locus.A, LocusPosition.Two, testDataA2);
            phenotypeInfo.A.Position2.Should().Be(testDataA2);


            const string testDataB1 = "[TEST] At locus B, position 1";
            phenotypeInfo.SetPosition(Locus.B, LocusPosition.One, testDataB1);
            phenotypeInfo.B.Position1.Should().Be(testDataB1);


            const string testDataB2 = "[TEST] At locus B, position 2";
            phenotypeInfo.SetPosition(Locus.B, LocusPosition.Two, testDataB2);
            phenotypeInfo.B.Position2.Should().Be(testDataB2);


            const string testDataC1 = "[TEST] At locus C, position 1";
            phenotypeInfo.SetPosition(Locus.C, LocusPosition.One, testDataC1);
            phenotypeInfo.C.Position1.Should().Be(testDataC1);


            const string testDataC2 = "[TEST] At locus C, position 2";
            phenotypeInfo.SetPosition(Locus.C, LocusPosition.Two, testDataC2);
            phenotypeInfo.C.Position2.Should().Be(testDataC2);


            const string testDataDrb11 = "[TEST] At locus Drb1, position 1";
            phenotypeInfo.SetPosition(Locus.Drb1, LocusPosition.One, testDataDrb11);
            phenotypeInfo.Drb1.Position1.Should().Be(testDataDrb11);


            const string testDataDrb12 = "[TEST] At locus Drb1, position 2";
            phenotypeInfo.SetPosition(Locus.Drb1, LocusPosition.Two, testDataDrb12);
            phenotypeInfo.Drb1.Position2.Should().Be(testDataDrb12);

            const string testDataDqb11 = "[TEST] At locus Dqb1, position 1";
            phenotypeInfo.SetPosition(Locus.Dqb1, LocusPosition.One, testDataDqb11);
            phenotypeInfo.Dqb1.Position1.Should().Be(testDataDqb11);


            const string testDataDqb12 = "[TEST] At locus Dqb1, position 2";
            phenotypeInfo.SetPosition(Locus.Dqb1, LocusPosition.Two, testDataDqb12);
            phenotypeInfo.Dqb1.Position2.Should().Be(testDataDqb12);


            const string testDataDpb11 = "[TEST] At locus Dpb1, position 1";
            phenotypeInfo.SetPosition(Locus.Dpb1, LocusPosition.One, testDataDpb11);
            phenotypeInfo.Dpb1.Position1.Should().Be(testDataDpb11);


            const string testDataDpb12 = "[TEST] At locus Dpb1, position 2";
            phenotypeInfo.SetPosition(Locus.Dpb1, LocusPosition.Two, testDataDpb12);
            phenotypeInfo.Dpb1.Position2.Should().Be(testDataDpb12);
        }

        [Test]
        public void Reduce_ReducesAllPositionValues()
        {
            var data = new PhenotypeInfo<int>
            {
                A = new LocusInfo<int>{ Position1 = 1, Position2 = 2},
                B = new LocusInfo<int>{ Position1 = 3, Position2 = 4},
                C = new LocusInfo<int>{ Position1 = 5, Position2 = 6},
                Dpb1 = new LocusInfo<int>{ Position1 = 7, Position2 = 8},
                Dqb1 = new LocusInfo<int>{ Position1 = 9, Position2 = 10},
                Drb1 = new LocusInfo<int>{ Position1 = 11, Position2 = 12},
            };

            var reducedValue = data.Reduce((locus, position, value, aggregator) => aggregator + value, 0);

            reducedValue.Should().Be(78);
        }

        [Test]
        public void Equality_WhenObjectsAreEqual_ReportsEquality()
        {
            var data1 = new PhenotypeInfo<int>
            {
                A = new LocusInfo<int> { Position1 = 1, Position2 = 2 },
                B = new LocusInfo<int> { Position1 = 3, Position2 = 4 },
                C = new LocusInfo<int> { Position1 = 5, Position2 = 6 },
                Dpb1 = new LocusInfo<int> { Position1 = 7, Position2 = 8 },
                Dqb1 = new LocusInfo<int> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int> { Position1 = 11, Position2 = 12 },
            };

            var data2 = new PhenotypeInfo<int>
            {
                A = new LocusInfo<int> { Position1 = 1, Position2 = 2 },
                B = new LocusInfo<int> { Position1 = 3, Position2 = 4 },
                C = new LocusInfo<int> { Position1 = 5, Position2 = 6 },
                Dpb1 = new LocusInfo<int> { Position1 = 7, Position2 = 8 },
                Dqb1 = new LocusInfo<int> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int> { Position1 = 11, Position2 = 12 },
            };

            data1.Equals(data2).Should().BeTrue();
            Equals(data1, data2).Should().BeTrue();
            (data1 == data2).Should().BeTrue();
            (data1 != data2).Should().BeFalse();


            data2.Equals(data1).Should().BeTrue();
            Equals(data2, data1).Should().BeTrue();
            (data2 == data1).Should().BeTrue();
            (data2 != data1).Should().BeFalse();

            ((object)data1).Equals(data2).Should().BeTrue();
            ((LociInfo<LocusInfo<int>>)data1).Equals(data2).Should().BeTrue();
            data1.Equals((object)data2).Should().BeTrue();
            data1.Equals((LociInfo<LocusInfo<int>>)data2).Should().BeTrue();

            data1.GetHashCode().Should().Be(data2.GetHashCode());
        }

        [Test]
        public void Equality_WhenObjectsAreDifferent_ReportsNonEquality()
        {
            var data1 = new PhenotypeInfo<int>
            {
                A = new LocusInfo<int> { Position1 = 1, Position2 = 2 },
                B = new LocusInfo<int> { Position1 = 3, Position2 = 4 },
                C = new LocusInfo<int> { Position1 = 5, Position2 = 6 },
                Dpb1 = new LocusInfo<int> { Position1 = 7, Position2 = 8 },
                Dqb1 = new LocusInfo<int> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int> { Position1 = 11, Position2 = 12 },
            };

            var data2 = new PhenotypeInfo<int>
            {
                A = new LocusInfo<int> { Position1 = 1, Position2 = 2 },
                B = new LocusInfo<int> { Position1 = 3, Position2 = 4 },
                C = new LocusInfo<int> { Position1 = 5, Position2 = 6 },
                Dpb1 = new LocusInfo<int> { Position1 = 7, Position2 = -1 }, //Different Here.
                Dqb1 = new LocusInfo<int> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int> { Position1 = 11, Position2 = 12 },
            };

            data1.Equals(data2).Should().BeFalse();
            Equals(data1, data2).Should().BeFalse();
            (data1 == data2).Should().BeFalse();
            (data1 != data2).Should().BeTrue();


            data2.Equals(data1).Should().BeFalse();
            Equals(data2, data1).Should().BeFalse();
            (data2 == data1).Should().BeFalse();
            (data2 != data1).Should().BeTrue();

            ((object)data1).Equals(data2).Should().BeFalse();
            ((LociInfo<LocusInfo<int>>)data1).Equals(data2).Should().BeFalse();
            data1.Equals((object)data2).Should().BeFalse();
            data1.Equals((LociInfo<LocusInfo<int>>)data2).Should().BeFalse();

            data1.GetHashCode().Should().NotBe(data2.GetHashCode());
        }

        [Test]
        public void LocusValues_CannotBeSetToNull()
        {
            var data = new PhenotypeInfo<int>();
            data.Invoking(d => { d.A = null; }).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
        }

        [Test]
        public void LocusValues_CannotBeInitialisedToNull()
        {
            Action nullInitialisation = () => new PhenotypeInfo<int>{ A = null };
            nullInitialisation.Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
        }

        [Test]
        public void Equality_WhenObjectsContainNulls_DoesNotError()
        {
            var same1 = new PhenotypeInfo<int?>
            {
                A = new LocusInfo<int?> { Position1 = null, Position2 = 2 },
                B = new LocusInfo<int?> { Position1 = 3, Position2 = null },
                C = new LocusInfo<int?> { Position1 = null, Position2 = null },
                //Dpb1 = ,
                Dqb1 = new LocusInfo<int?> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int?> { Position1 = 11, Position2 = 12 },
            };

            var same2 = new PhenotypeInfo<int?>
            {
                A = new LocusInfo<int?> { Position1 = null, Position2 = 2 },
                B = new LocusInfo<int?> { Position1 = 3, Position2 = null },
                C = new LocusInfo<int?> { Position1 = null, Position2 = null },
                //Dpb1 = ,
                Dqb1 = new LocusInfo<int?> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int?> { Position1 = 11, Position2 = 12 },
            };

            var different = new PhenotypeInfo<int?>
            {
                A = new LocusInfo<int?> { Position1 = 1, Position2 = null },
                B = new LocusInfo<int?> { Position1 = 3, Position2 = null },
                //C = ,
                Dpb1 = new LocusInfo<int?> { Position1 = null, Position2 = null },
                Dqb1 = new LocusInfo<int?> { Position1 = null, Position2 = null },
                //Drb1 = ,
            };

            same1.Equals(same2).Should().BeTrue();
            Equals(same1, same2).Should().BeTrue();
            (same1 == same2).Should().BeTrue();
            (same1 != same2).Should().BeFalse();


            same2.Equals(same1).Should().BeTrue();
            Equals(same2, same1).Should().BeTrue();
            (same2 == same1).Should().BeTrue();
            (same2 != same1).Should().BeFalse();

            ((object)same1).Equals(same2).Should().BeTrue();
            ((LociInfo<LocusInfo<int?>>)same1).Equals(same2).Should().BeTrue();
            same1.Equals((object)same2).Should().BeTrue();
            same1.Equals((LociInfo<LocusInfo<int?>>)same2).Should().BeTrue();

            same1.GetHashCode().Should().Be(same2.GetHashCode());




            same1.Equals(different).Should().BeFalse();
            Equals(same1, different).Should().BeFalse();
            (same1 == different).Should().BeFalse();
            (same1 != different).Should().BeTrue();


            different.Equals(same1).Should().BeFalse();
            Equals(different, same1).Should().BeFalse();
            (different == same1).Should().BeFalse();
            (different != same1).Should().BeTrue();

            ((object)same1).Equals(different).Should().BeFalse();
            ((LociInfo<LocusInfo<int?>>)same1).Equals(different).Should().BeFalse();
            same1.Equals((object)different).Should().BeFalse();
            same1.Equals((LociInfo<LocusInfo<int?>>)different).Should().BeFalse();

            same1.GetHashCode().Should().NotBe(different.GetHashCode());

        }
    }
}