using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Helpers;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeAlleleTruncater
    {
        public PhenotypeInfo<string> TruncateGenotypeAlleles(PhenotypeInfo<string> genotype);
    }

    public class GenotypeAlleleTruncater : IGenotypeAlleleTruncater
    {
        public PhenotypeInfo<string> TruncateGenotypeAlleles(PhenotypeInfo<string> genotype)
        {
            var truncatedGenotype = new PhenotypeInfo<string>();

            genotype.EachPosition((locus, position, allele) =>
            {
                if (allele == null)
                {
                    return;
                }

                var truncatedAllele = AlleleSplitter.FirstTwoFieldsAsString(allele);
                truncatedGenotype.SetPosition(locus, position, truncatedAllele);
            });

            return truncatedGenotype;
        }
    }
}