using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Common.Models.SearchResults
{
    public class ScoreResult
    {
        public int GradeScore => LocusScoreDetails.Sum(scoreDetails => scoreDetails.MatchGradeScore);
        public int ConfidenceScore => LocusScoreDetails.Sum(scoreDetails => scoreDetails.MatchConfidenceScore);
        
        public LocusScoreDetails ScoreDetailsAtLocusA { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusB { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusC { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusDrb1 { get; set; }
        public LocusScoreDetails ScoreDetailsAtLocusDqb1 { get; set; }
        
        private IEnumerable<LocusScoreDetails> LocusScoreDetails => new List<LocusScoreDetails>
        {
            ScoreDetailsAtLocusA,
            ScoreDetailsAtLocusB,
            ScoreDetailsAtLocusC,
            ScoreDetailsAtLocusDqb1,
            ScoreDetailsAtLocusDrb1
        };
        
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
                case Locus.Dqb1:
                    ScoreDetailsAtLocusDqb1 = locusScoreDetails;
                    break;
                case Locus.Drb1:
                    ScoreDetailsAtLocusDrb1 = locusScoreDetails;
                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}