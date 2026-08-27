using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    /// <summary>
    /// ATL-233: these were instance methods on <see cref="LocusInfo{T}"/> and are now extension methods constrained
    /// to a reference T. The behaviour below is unchanged - the point of the move is what it makes impossible, and
    /// that cannot be asserted here: a value-type T no longer compiles, so there is no test to write for it.
    /// </summary>
    [TestFixture]
    public class LocusInfoExtensionsTests
    {
        private Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
        }

        [Test]
        public void Position1And2Null_WhenNeitherPositionSet_ReturnsTrue()
        {
            new LocusInfo<string>().Position1And2Null().Should().BeTrue();
        }

        [Test]
        public void Position1And2Null_WhenOnlyPosition1Set_ReturnsFalse()
        {
            new LocusInfo<string>(fixture.Create<string>(), null).Position1And2Null().Should().BeFalse();
        }

        [Test]
        public void Position1And2Null_WhenOnlyPosition2Set_ReturnsFalse()
        {
            new LocusInfo<string>(null, fixture.Create<string>()).Position1And2Null().Should().BeFalse();
        }

        [Test]
        public void Position1And2Null_WhenBothPositionsSet_ReturnsFalse()
        {
            new LocusInfo<string>(fixture.Create<string>(), fixture.Create<string>()).Position1And2Null().Should().BeFalse();
        }

        [Test]
        public void Position1And2NotNull_WhenBothPositionsSet_ReturnsTrue()
        {
            new LocusInfo<string>(fixture.Create<string>(), fixture.Create<string>()).Position1And2NotNull().Should().BeTrue();
        }

        [Test]
        public void Position1And2NotNull_WhenOnlyPosition1Set_ReturnsFalse()
        {
            new LocusInfo<string>(fixture.Create<string>(), null).Position1And2NotNull().Should().BeFalse();
        }

        [Test]
        public void Position1And2NotNull_WhenOnlyPosition2Set_ReturnsFalse()
        {
            new LocusInfo<string>(null, fixture.Create<string>()).Position1And2NotNull().Should().BeFalse();
        }

        [Test]
        public void Position1And2NotNull_WhenNeitherPositionSet_ReturnsFalse()
        {
            new LocusInfo<string>().Position1And2NotNull().Should().BeFalse();
        }

        [Test]
        public void SinglePositionNull_WhenOnlyPosition1Set_ReturnsTrue()
        {
            new LocusInfo<string>(fixture.Create<string>(), null).SinglePositionNull().Should().BeTrue();
        }

        [Test]
        public void SinglePositionNull_WhenOnlyPosition2Set_ReturnsTrue()
        {
            new LocusInfo<string>(null, fixture.Create<string>()).SinglePositionNull().Should().BeTrue();
        }

        [Test]
        public void SinglePositionNull_WhenBothPositionsSet_ReturnsFalse()
        {
            new LocusInfo<string>(fixture.Create<string>(), fixture.Create<string>()).SinglePositionNull().Should().BeFalse();
        }

        [Test]
        public void SinglePositionNull_WhenNeitherPositionSet_ReturnsFalse()
        {
            new LocusInfo<string>().SinglePositionNull().Should().BeFalse();
        }

        /// <summary>
        /// Every case above uses <c>string</c>. This one pins that the probes are generic over any reference
        /// <c>T</c>, with <c>IEnumerable&lt;string&gt;</c> chosen because an interface sits furthest from
        /// <c>string</c>. Two live call sites are of that shape - <c>LocusMatchCalculator</c> over
        /// <c>LocusInfo&lt;IEnumerable&lt;string&gt;&gt;</c> and <c>PositionalScorerBase</c> over
        /// <c>LocusInfo&lt;IHlaScoringMetadata&gt;</c>.
        ///
        /// <para>
        /// This says nothing about the <c>class?</c> constraint, and cannot: plain <c>class</c> admits an interface
        /// too. The <c>?</c> is there because these methods exist to ask whether a position is absent, and
        /// <c>class</c> would declare <c>T</c> non-nullable - so a nullable-enabled caller holding
        /// <c>LocusInfo&lt;string?&gt;</c> would take CS8634 for asking exactly that. No call site needs the
        /// <c>?</c> today; every calling project is nullable-oblivious.
        /// </para>
        /// </summary>
        [Test]
        public void NullProbes_WhenTypeParameterIsAnyReferenceType_ProbeTheValues()
        {
            var locusInfo = new LocusInfo<IEnumerable<string>>(fixture.CreateMany<string>(2), null);

            locusInfo.Position1And2Null().Should().BeFalse();
            locusInfo.Position1And2NotNull().Should().BeFalse();
            locusInfo.SinglePositionNull().Should().BeTrue();
        }
    }
}
