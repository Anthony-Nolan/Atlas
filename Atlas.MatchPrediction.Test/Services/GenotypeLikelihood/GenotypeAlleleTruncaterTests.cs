using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.GenotypeLikelihood
{
    [TestFixture]
    public class GenotypeAlleleTruncaterTests
    {
        private IGenotypeAlleleTruncater alleleTruncater;

        [SetUp]
        public void SetUp()
        {
            alleleTruncater = new GenotypeAlleleTruncater();
        }

        [Test]
        public void TruncateGenotypeAlleles_WhenGenotypeOnlyHasTwoFieldAlleles_ReturnsUnmodifiedGenotype()
        {
            var genotype = TwoFieldGenotypeBuilder.Build();

            var actualGenotype = alleleTruncater.TruncateGenotypeAlleles(genotype);

            actualGenotype.Should().BeEquivalentTo(genotype);
        }


        [TestCase("ExtraField")]
        [TestCase("ExtraField:ExtraField")]
        public void TruncateGenotypeAlleles_WhenWholeGenotypeHasThreeOrFourFieldAllele_ReturnsGenotypeTruncatedToTwoFields(string fieldsToAdd)
        {
            var genotype = TwoFieldGenotypeBuilder.Build();
            var genotypeWithAddedFields = genotype.Map((locus, position, allele) => $"{genotype.GetPosition(locus, position)}:{fieldsToAdd}");

            var actualGenotype = alleleTruncater.TruncateGenotypeAlleles(genotypeWithAddedFields);

            actualGenotype.Should().BeEquivalentTo(genotype);
        }

        [TestCase("ExtraField", Locus.A)]
        [TestCase("ExtraField:ExtraField", Locus.A)]
        [TestCase("ExtraField", Locus.B)]
        [TestCase("ExtraField:ExtraField", Locus.B)]
        [TestCase("ExtraField", Locus.C)]
        [TestCase("ExtraField:ExtraField", Locus.C)]
        [TestCase("ExtraField", Locus.Dqb1)]
        [TestCase("ExtraField:ExtraField", Locus.Dqb1)]
        [TestCase("ExtraField", Locus.Drb1)]
        [TestCase("ExtraField:ExtraField", Locus.Drb1)]
        public void TruncateGenotypeAlleles_WhenGenotypeHasThreeOrFourFieldAllele_ReturnsGenotypeTruncatedToTwoFields(string fieldsToAdd, Locus locus)
        {
            var genotype = TwoFieldGenotypeBuilder.Build();

            genotype = genotype.SetPosition(locus, LocusPosition.One, $"{genotype.GetPosition(locus, LocusPosition.One)}:{fieldsToAdd}")
                .SetPosition(locus, LocusPosition.Two, $"{genotype.GetPosition(locus, LocusPosition.Two)}:{fieldsToAdd}");

            var actualGenotype = alleleTruncater.TruncateGenotypeAlleles(genotype);

            actualGenotype.Should().BeEquivalentTo(TwoFieldGenotypeBuilder.Build());
        }

        private PhenotypeInfoBuilder<string> TwoFieldGenotypeBuilder =>
            new PhenotypeInfoBuilder<string>(new PhenotypeInfo<string>("FirstField:SecondField"));
    }
}