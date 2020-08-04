using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Utils
{
    internal static class PhenotypeInfoExtensions
    {
        /// <summary>
        /// If one of two positions is null copies over expressing alleles to null positions.
        /// This only works if the data represents P Groups.
        /// </summary>
        public static void CopyExpressingAllelesToNullPositions(this PhenotypeInfo<HlaAtKnownTypingCategory> typedGenotype)
        {
            typedGenotype.EachLocus((locus, locusInfo) =>
            {
                if (locusInfo.SinglePositionNull())
                {
                    if (locusInfo.Position1 == null && locusInfo.Position2.TypingCategory == HaplotypeTypingCategory.PGroup)
                    {
                        typedGenotype.SetPosition(locus, LocusPosition.One, locusInfo.Position2);
                    }
                    if (locusInfo.Position2 == null && locusInfo.Position1.TypingCategory == HaplotypeTypingCategory.PGroup)
                    {
                        typedGenotype.SetPosition(locus, LocusPosition.Two, locusInfo.Position1);
                    }
                }
            });
        }
    }
}
