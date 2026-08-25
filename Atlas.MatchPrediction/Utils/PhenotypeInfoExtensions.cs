using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

// The alias states at the declaration what the "*MUST* be typed to P Group resolution" comment below asks for. An
// alias rather than a PGroup wrapper type, because a wrapper on this path has a cost that has not been measured.
using PGroupGenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Utils
{
    internal static class PhenotypeInfoExtensions
    {
        /// <summary>
        /// If one of two positions is null copies over expressing alleles to null positions.
        /// Input hla *MUST* be typed to P Group resolution.
        /// </summary>
        public static PGroupGenotype CopyExpressingAllelesToNullPositions(this PGroupGenotype typedGenotype)
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
