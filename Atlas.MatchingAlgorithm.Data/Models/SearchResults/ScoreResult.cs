using System;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Data.Models.SearchResults
{
    public class ScoreResult
    {
        public LocusScoreDetails ScoreDetailsAtLocusA { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusB { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusC { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusDpb1 { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusDqb1 { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusDrb1 { get; set; }
        
        /// <summary>
        /// Contains aggregate scoring data across all loci. Should only be populated once scoring is complete at all loci.
        /// </summary>
        public AggregateScoreDetails AggregateScoreDetails { get; set; }

        public LocusScoreDetails ScoreDetailsForLocus(Locus locus)
        {
            return locus switch
            {
                Locus.A => ScoreDetailsAtLocusA,
                Locus.B => ScoreDetailsAtLocusB,
                Locus.C => ScoreDetailsAtLocusC,
                Locus.Dpb1 => ScoreDetailsAtLocusDpb1,
                Locus.Dqb1 => ScoreDetailsAtLocusDqb1,
                Locus.Drb1 => ScoreDetailsAtLocusDrb1,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        public void SetScoreDetailsForLocus(Locus locus, LocusScoreDetails locusScoreDetails)
        {
            switch (locus)
            {
                case Locus.A:
                    ScoreDetailsAtLocusA = locusScoreDetails;
                    break;
                case Locus.B:
                    ScoreDetailsAtLocusB = locusScoreDetails;
                    break;
                case Locus.C:
                    ScoreDetailsAtLocusC = locusScoreDetails;
                    break;
                case Locus.Dpb1:
                    ScoreDetailsAtLocusDpb1 = locusScoreDetails;
                    break;
                case Locus.Dqb1:
                    ScoreDetailsAtLocusDqb1 = locusScoreDetails;
                    break;
                case Locus.Drb1:
                    ScoreDetailsAtLocusDrb1 = locusScoreDetails;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}