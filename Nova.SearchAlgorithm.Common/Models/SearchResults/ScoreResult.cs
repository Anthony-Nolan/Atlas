using System;

namespace Nova.SearchAlgorithm.Common.Models.SearchResults
{
    public class ScoreResult
    {
        public int TotalMatchRank;
        public int TotalMatchGrade;
        public int TotalMatchConfidence;
        
        public LocusScoreDetails ScoreDetailsAtLocusA { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusB { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusC { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusDrb1 { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusDqb1 { get; set; }
        
        public LocusScoreDetails ScoreDetailsForLocus(Locus locus)
        {
            switch (locus)
            {
                case Locus.A:
                    return ScoreDetailsAtLocusA;
                case Locus.B:
                    return ScoreDetailsAtLocusB;
                case Locus.C:
                    return ScoreDetailsAtLocusC;
                case Locus.Dqb1:
                    return ScoreDetailsAtLocusDqb1;
                case Locus.Drb1:
                    return ScoreDetailsAtLocusDrb1;
                case Locus.Dpb1:
                    // TODO: NOVA-1301 implement scoring for Dpb1
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}