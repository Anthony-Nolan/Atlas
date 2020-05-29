using Atlas.Common.GeneticData.PhenotypeInfo;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Core.PhenotypeInfo
{
    public class DiplotypeInfoTests
    {
        [Test]
        public void CheckEquality_WhenDiplotypesHaveSameHaplotypes_ShouldBeTrue()
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

            var lociInfo3 = new LociInfo<string>() {A = "A-2", B = "B-2", C = "C-2", Dqb1 = "Dqb1-2", Drb1 = "Drb1-2"};
            var lociInfo4 = new LociInfo<string>() {A = "A-1", B = "B-1", C = "C-1", Dqb1 = "Dqb1-1", Drb1 = "Drb1-1"};

            var diplotype1 = new DiplotypeInfo<string>
            {
                Haplotype1 = lociInfo1,
                Haplotype2 = lociInfo2
            };

            var diplotype2 = new DiplotypeInfo<string>
            {
                Haplotype1 = lociInfo3,
                Haplotype2 = lociInfo4
            };

            diplotype1.Should().NotBeEquivalentTo(diplotype2);
            diplotype1.GetHashCode().Should().NotBe(diplotype2.GetHashCode());
        }
    }
}
