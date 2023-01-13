using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    internal interface IGenotypeAlleleTruncater
    {
        /// <summary>
        /// This will only be used when we implement 2-field HF sets.
        /// </summary>
        public PhenotypeInfo<string> TruncateGenotypeAlleles(PhenotypeInfo<string> genotype);
    }

    internal class GenotypeAlleleTruncater : IGenotypeAlleleTruncater
    {
        public PhenotypeInfo<string> TruncateGenotypeAlleles(PhenotypeInfo<string> genotype)
        {
            return genotype.Map((locus, position, allele) =>
                allele == null ? null : AlleleSplitter.FirstTwoFieldsAsString(allele));
        }
    }
}