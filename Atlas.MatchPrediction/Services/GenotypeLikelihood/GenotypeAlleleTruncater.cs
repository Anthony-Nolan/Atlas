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
            return genotype.Map((locus, position, allele) =>
                allele == null ? null : AlleleSplitter.FirstTwoFieldsAsString(allele));
        }
    }
}