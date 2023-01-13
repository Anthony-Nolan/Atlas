using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using EnumStringValues;
using FluentAssertions;
using MoreLinq.Extensions;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    [TestFixture]
    public class LociInfoTests
    {
        private readonly IEnumerable<Locus> supportedLoci = EnumExtensions.EnumerateValues<Locus>();

        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Map_ReturnsMappedLociInfo()
        {
            static string Mapping(string locusValue) => $"Mapped {locusValue}";

            var initial = new LociInfo<string>();
            foreach (var locus in supportedLoci)
            {
                initial = initial.SetLocus(locus, $"TEST-{locus.ToString()}");
            }

            var mapped = initial.Map(Mapping);

            mapped.A.Should().Be(Mapping(initial.A));
            mapped.B.Should().Be(Mapping(initial.B));
            mapped.C.Should().Be(Mapping(initial.C));
            mapped.Dpb1.Should().Be(Mapping(initial.Dpb1));
            mapped.Dqb1.Should().Be(Mapping(initial.Dqb1));
            mapped.Drb1.Should().Be(Mapping(initial.Drb1));
        }

        [Test]
        public void Map_WhenMapperTakesLocus_CallsMapperForEachLocusAndReturnsMappedLociInfo()
        {
            static string Mapping(Locus locus, string locusValue)
            {
                return $"Mapped {locusValue} at {locus.ToString()}";
            }

            var initial = new LociInfo<string>();
            foreach (var locus in supportedLoci)
            {
                initial = initial.SetLocus(locus, $"TEST-{locus.ToString()}");
            }

            var mapped = initial.Map(Mapping);

            mapped.A.Should().Be(Mapping(Locus.A, initial.A));
            mapped.B.Should().Be(Mapping(Locus.B, initial.B));
            mapped.C.Should().Be(Mapping(Locus.C, initial.C));
            mapped.Dpb1.Should().Be(Mapping(Locus.Dpb1, initial.Dpb1));
            mapped.Dqb1.Should().Be(Mapping(Locus.Dqb1, initial.Dqb1));
            mapped.Drb1.Should().Be(Mapping(Locus.Drb1, initial.Drb1));
        }

        [Test]
        public void Reduce_ReducesAllLoci()
        {
            var data = new LociInfo<int>(1, 2, 3, 4, 5, 6);

            var reducedData = data.Reduce((locus, value, accumulator) => accumulator + value, 0);

            reducedData.Should().Be(21);
        }

        [Test]
        public void AnyAtLoci_WhenOnlyExcludedLociReturnTrue_ReturnsFalse()
        {
            const Locus excludedLocus = Locus.C;
            var lociInfo = new LociInfoBuilder<bool>(false).WithDataAt(excludedLocus, true).Build();

            lociInfo.AnyAtLoci(x => x, supportedLoci.Except(excludedLocus).ToHashSet()).Should().BeFalse();
        }

        [Test]
        public void AnyAtLoci_WhenSingleLocusReturnsTrue_ReturnsTrue()
        {
            const Locus includedLocus = Locus.C;
            var lociInfo = new LociInfoBuilder<bool>(false).WithDataAt(includedLocus, true).Build();

            lociInfo.AnyAtLoci(x => x, new HashSet<Locus> {includedLocus}).Should().BeTrue();
        }

        [Test]
        public void AllAtLoci_WhenSingleLocusReturnsFalse_ReturnsFalse()
        {
            const Locus includedLocus = Locus.Dqb1;
            var lociInfo = new LociInfoBuilder<bool>(true).WithDataAt(includedLocus, false).Build();

            lociInfo.AllAtLoci(x => x, new HashSet<Locus> {includedLocus}).Should().BeFalse();
        }

        [Test]
        public void AllAtLoci_WhenOnlyExcludedLocusReturnsFalse_ReturnsTrue()
        {
            const Locus excludedLocus = Locus.Dqb1;
            var lociInfo = new LociInfoBuilder<bool>(true).WithDataAt(excludedLocus, false).Build();

            lociInfo.AllAtLoci(x => x, supportedLoci.Except(excludedLocus).ToHashSet()).Should().BeTrue();
        }

        [Test]
        public void AllAtLoci_WhenAllLociReturnTrue_ReturnsTrue()
        {
            var lociInfo = new LociInfoBuilder<bool>(true).Build();

            lociInfo.AllAtLoci(x => x, supportedLoci.ToHashSet()).Should().BeTrue();
        }
    }
}