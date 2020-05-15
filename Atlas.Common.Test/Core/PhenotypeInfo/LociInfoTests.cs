using System.Collections.Generic;
using FluentAssertions;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    [TestFixture]
    public class LociInfoTests
    {
        private readonly IEnumerable<LocusType> supportedLoci = new List<LocusType>
        {
            LocusType.A,
            LocusType.B,
            LocusType.C,
            LocusType.Dpa1,
            LocusType.Dpb1,
            LocusType.Dqa1,
            LocusType.Dqb1,
            LocusType.Drb1,
            LocusType.Drb3,
            LocusType.Drb4,
            LocusType.Drb5
        };

        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Map_ReturnsMappedLociInfo()
        {
            string Mapping(string locusValue) => $"Mapped {locusValue}";

            var initial = new LociInfo<string>();
            foreach (var locus in supportedLoci)
            {
                initial.SetLocus(locus, $"TEST-{locus.ToString()}");
            }

            var mapped = initial.Map(Mapping);

            mapped.A.Should().Be(Mapping(initial.A));
            mapped.B.Should().Be(Mapping(initial.B));
            mapped.C.Should().Be(Mapping(initial.C));
            mapped.Dpa1.Should().Be(Mapping(initial.Dpa1));
            mapped.Dpb1.Should().Be(Mapping(initial.Dpb1));
            mapped.Dqa1.Should().Be(Mapping(initial.Dqa1));
            mapped.Dqb1.Should().Be(Mapping(initial.Dqb1));
            mapped.Drb1.Should().Be(Mapping(initial.Drb1));
            mapped.Drb3.Should().Be(Mapping(initial.Drb3));
            mapped.Drb4.Should().Be(Mapping(initial.Drb4));
            mapped.Drb5.Should().Be(Mapping(initial.Drb5));
        }

        [Test]
        public void Map_WhenMapperTakesLocus_CallsMapperForEachLocusAndReturnsMappedLociInfo()
        {
            string Mapping(LocusType locusType, string locusValue)
            {
                return $"Mapped {locusValue} at {locusType.ToString()}";
            }

            var initial = new LociInfo<string>();
            foreach (var locus in supportedLoci)
            {
                initial.SetLocus(locus, $"TEST-{locus.ToString()}");
            }

            var mapped = initial.Map(Mapping);

            mapped.A.Should().Be(Mapping(LocusType.A, initial.A));
            mapped.B.Should().Be(Mapping(LocusType.B, initial.B));
            mapped.C.Should().Be(Mapping(LocusType.C, initial.C));
            mapped.Dpa1.Should().Be(Mapping(LocusType.Dpa1, initial.Dpa1));
            mapped.Dpb1.Should().Be(Mapping(LocusType.Dpb1, initial.Dpb1));
            mapped.Dqa1.Should().Be(Mapping(LocusType.Dqa1, initial.Dqa1));
            mapped.Dqb1.Should().Be(Mapping(LocusType.Dqb1, initial.Dqb1));
            mapped.Drb1.Should().Be(Mapping(LocusType.Drb1, initial.Drb1));
            mapped.Drb3.Should().Be(Mapping(LocusType.Drb3, initial.Drb3));
            mapped.Drb4.Should().Be(Mapping(LocusType.Drb4, initial.Drb4));
            mapped.Drb5.Should().Be(Mapping(LocusType.Drb5, initial.Drb5));
        }
    }
}