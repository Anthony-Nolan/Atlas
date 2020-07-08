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
            phenotypeInfo.Invoking(p => p.A = null).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            phenotypeInfo.Invoking(p => p.B = null).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            phenotypeInfo.Invoking(p => p.C = null).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            phenotypeInfo.Invoking(p => p.Dpb1 = null).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            phenotypeInfo.Invoking(p => p.Dqb1 = null).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            phenotypeInfo.Invoking(p => p.Drb1 = null).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
        }

        [Test]
        public void LocusValues_CannotBeInitialisedToNull()
        {
            // ReSharper disable ObjectCreationAsStatement
            ((Action)(() => new PhenotypeInfo<int> { A = null })).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            ((Action)(() => new PhenotypeInfo<int> { B = null })).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            ((Action)(() => new PhenotypeInfo<int> { C = null })).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            ((Action)(() => new PhenotypeInfo<int> { Dpb1 = null })).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            ((Action)(() => new PhenotypeInfo<int> { Dqb1 = null })).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            ((Action)(() => new PhenotypeInfo<int> { Drb1 = null })).Should().Throw<ArgumentNullException>().WithMessage("*cannot be null*");
            // ReSharper restore ObjectCreationAsStatement
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
            var original = new PhenotypeInfo<int>
            {
                A = new LocusInfo<int> { Position1 = 1, Position2 = 2 },
                B = new LocusInfo<int> { Position1 = 3, Position2 = 4 },
                C = new LocusInfo<int> { Position1 = 5, Position2 = 6 },
                Dpb1 = new LocusInfo<int> { Position1 = 7, Position2 = 8 },
                Dqb1 = new LocusInfo<int> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int> { Position1 = 11, Position2 = 12 },
            };

            var copy = new PhenotypeInfo<int>
            {
                A = new LocusInfo<int> { Position1 = 1, Position2 = 2 },
                B = new LocusInfo<int> { Position1 = 3, Position2 = 4 },
                C = new LocusInfo<int> { Position1 = 5, Position2 = 6 },
                Dpb1 = new LocusInfo<int> { Position1 = 7, Position2 = 8 },
                Dqb1 = new LocusInfo<int> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int> { Position1 = 11, Position2 = 12 },
            };

            AllVariationsOnObjectEqualityShouldReport(original, copy, true);
        }

        [Test]
        public void Equality_WhenObjectsAreDifferent_ReportsNonEquality()
        {
            var original = new PhenotypeInfo<int>
            {
                A = new LocusInfo<int> { Position1 = 1, Position2 = 2 },
                B = new LocusInfo<int> { Position1 = 3, Position2 = 4 },
                C = new LocusInfo<int> { Position1 = 5, Position2 = 6 },
                Dpb1 = new LocusInfo<int> { Position1 = 7, Position2 = 8 },
                Dqb1 = new LocusInfo<int> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int> { Position1 = 11, Position2 = 12 },
            };

            var different = new PhenotypeInfo<int>
            {
                A = new LocusInfo<int> { Position1 = 1, Position2 = 2 },
                B = new LocusInfo<int> { Position1 = 3, Position2 = 4 },
                C = new LocusInfo<int> { Position1 = 5, Position2 = 6 },
                Dpb1 = new LocusInfo<int> { Position1 = 7, Position2 = -1 }, //Different Here.
                Dqb1 = new LocusInfo<int> { Position1 = 9, Position2 = 10 },
                Drb1 = new LocusInfo<int> { Position1 = 11, Position2 = 12 },
            };

            AllVariationsOnObjectEqualityShouldReport(original, different, false);
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
                A = new LocusInfo<int?> { Position1 = 1, Position2 = null },       // nullity is inverse of same1
                B = new LocusInfo<int?> { Position1 = 3, Position2 = null },       // nullity matches same1
                //C = ,                                                            // This default, same1 all null
                Dpb1 = new LocusInfo<int?> { Position1 = null, Position2 = null }, // This all null, same1 default
                Dqb1 = new LocusInfo<int?> { Position1 = null, Position2 = null },
                //Drb1 = ,
            };

            AllVariationsOnObjectEqualityShouldReport(same1, same2, true);
            AllVariationsOnObjectEqualityShouldReport(same1, different, false);
            AllVariationsOnObjectEqualityShouldReport(same2, different, false);
        }

        public void AllVariationsOnObjectEqualityShouldReport<T>(PhenotypeInfo<T> first, PhenotypeInfo<T> second, bool areEqual)
        {
            first.Equals(second).Should().Be(areEqual);
            Equals(first, second).Should().Be(areEqual);
            (first == second).Should().Be(areEqual);
            (first != second).Should().Be(!areEqual);


            second.Equals(first).Should().Be(areEqual);
            Equals(second, first).Should().Be(areEqual);
            (second == first).Should().Be(areEqual);
            (second != first).Should().Be(!areEqual);

            ((object)first).Equals(second).Should().Be(areEqual);
            ((LociInfo<LocusInfo<T>>)first).Equals(second).Should().Be(areEqual);
            first.Equals((object)second).Should().Be(areEqual);
            first.Equals((LociInfo<LocusInfo<T>>)second).Should().Be(areEqual);

            if (areEqual)
            {
                first.GetHashCode().Should().Be(second.GetHashCode());
            }
            else
            {
                first.GetHashCode().Should().NotBe(second.GetHashCode());
            }
        }

    }
}