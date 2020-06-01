using Atlas.Common.GeneticData.PhenotypeInfo;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    public class DiplotypeInfoTests
    {
        [Test]
        public void CheckEquality_WhenDiplotypesHaveSameHaplotypesInSameOrder_ShouldBeTrue()
        {
            var lociInfo1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"};
            var lociInfo2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"};

            var diplotype1 = new DiplotypeInfo<string>
            {
                Haplotype1 = lociInfo1,
                Haplotype2 = lociInfo2
            };

            var diplotype2 = new DiplotypeInfo<string>
            {
                Haplotype1 = lociInfo1,
                Haplotype2 = lociInfo2
            };

            diplotype1.Should().BeEquivalentTo(diplotype2);
            diplotype1.GetHashCode().Should().Be(diplotype2.GetHashCode());
        }

        [Test]
        public void CheckEquality_WhenDiplotypesHaveSameHaplotypesSwapped_ShouldBeTrue()
        {
            var lociInfo1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"};
            var lociInfo2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"};

            var diplotype1 = new DiplotypeInfo<string>
            {
                Haplotype1 = lociInfo1,
                Haplotype2 = lociInfo2
            };

            var diplotype2 = new DiplotypeInfo<string>
            {
                Haplotype1 = lociInfo2,
                Haplotype2 = lociInfo1
            };

            diplotype1.Should().BeEquivalentTo(diplotype2);
            diplotype1.GetHashCode().Should().Be(diplotype2.GetHashCode());
        }

        [Test]
        public void CheckEquality_WhenDiplotypesHaveDifferentHaplotypes_ShouldBeFalse()
        {
            var lociInfo1 = new LociInfo<string>() {A = "A-2", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"};
            var lociInfo2 = new LociInfo<string>() {A = "A-1", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"};

            var differentLociInfo1 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"};
            var differentLociInfo2 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"};

            var diplotype1 = new DiplotypeInfo<string>
            {
                Haplotype1 = lociInfo1,
                Haplotype2 = lociInfo2
            };

            var diplotype2 = new DiplotypeInfo<string>
            {
                Haplotype1 = differentLociInfo1,
                Haplotype2 = differentLociInfo2
            };

            diplotype1.Should().NotBeEquivalentTo(diplotype2);
            diplotype1.GetHashCode().Should().NotBe(diplotype2.GetHashCode());
        }

        [Test]
        public void SetAtLocus_WhenLocusInfoIsParsed_ReturnsExpectedDiplotype()
        {
            var lociInfo = new LociInfo<LocusInfo<string>>
            {
                A = new LocusInfo<string> {Position1 = "A-1", Position2 = "A-2"},
                B = new LocusInfo<string> {Position1 = "B-1", Position2 = "B-2"},
                C = new LocusInfo<string> {Position1 = "C-1", Position2 = "C-2"},
                Dqb1 = new LocusInfo<string> {Position1 = "Dqb1-1", Position2 = "Dqb1-2"},
                Drb1 = new LocusInfo<string> {Position1 = "Drb1-1", Position2 = "Drb1-2"}
            };

            var actualDiplotype = new DiplotypeInfo<string>(lociInfo);

            var expectedDiplotype = new DiplotypeInfo<string>
            {
                Haplotype1 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"},
                Haplotype2 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"}
            };

            actualDiplotype.Should().BeEquivalentTo(expectedDiplotype);
        }
    }
}
