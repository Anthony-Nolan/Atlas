using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Utils
{
    internal static class PhenotypeInfoExtensions
    {
        /// <summary>
        /// If one of two positions is null copies over expressing alleles to null positions.
        /// Input hla *MUST* be typed to P Group resolution.
        /// </summary>
        public static PhenotypeInfo<string> CopyExpressingAllelesToNullPositions(this PhenotypeInfo<string> typedGenotype)
        {
            typedGenotype.EachLocus((locus, locusInfo) =>
            {
                if (locusInfo.SinglePositionNull())
                {
                    if (locusInfo.Position1 == null)
                    {
                        typedGenotype.SetPosition(locus, LocusPosition.One, locusInfo.Position2);
                    }
                    if (locusInfo.Position2 == null)
                    {
                        typedGenotype.SetPosition(locus, LocusPosition.Two, locusInfo.Position1);
                    }
                }
            });

            return typedGenotype;
        }
    }
}
