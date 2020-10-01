using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;

namespace Atlas.MatchingAlgorithm.Common.Models.SearchResults
{
    public class LocusMatchDetails
    {
        /// <summary>
        /// The number of matches within this locus.
        /// Either 0, 1 or 2 if the locus is typed.
        /// If the locus is not typed this will be 2, since there is a potential match.
        /// </summary>
        public int MatchCount
        {
            get {
                if (!PositionPairs.Any())
                {
                    return 0;
                }

                if (DirectMatch() || CrossMatch())
                {
                    return 2;
                }

                return 1;
            }
        }

        private bool CrossMatch()
        {
            return PositionPairs.Contains((LocusPosition.One, LocusPosition.Two)) && PositionPairs.Contains((LocusPosition.Two, LocusPosition.One));
        }

        private bool DirectMatch()
        {
            return PositionPairs.Contains((LocusPosition.One, LocusPosition.One)) && PositionPairs.Contains((LocusPosition.Two, LocusPosition.Two));
        }

        /// <summary>
        /// Lazily populated sets of known pairs of positions. Once fully populated, can be used to determine the MatchCount.
        ///
        /// (Search, Matching) [i.e. (Patient, Donor)]
        /// </summary>
        public HashSet<(LocusPosition, LocusPosition)> PositionPairs { get; set; } = new HashSet<(LocusPosition, LocusPosition)>();
    }
}