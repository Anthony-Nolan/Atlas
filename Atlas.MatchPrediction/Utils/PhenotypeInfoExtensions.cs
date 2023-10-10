using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

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
            return typedGenotype.MapByLocus((_, locusInfo) =>
            {
                if (locusInfo.SinglePositionNull())
                {
                    if (locusInfo.Position1 == null)
                    {
                        locusInfo = locusInfo.SetAtPosition(LocusPosition.One, locusInfo.Position2);
                    }
                    if (locusInfo.Position2 == null)
                    {
                        locusInfo = locusInfo.SetAtPosition(LocusPosition.Two, locusInfo.Position1);
                    }
                }

                return locusInfo;
            });
        }
    }
}
