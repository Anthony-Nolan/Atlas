using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Common.Models
{
    public class PotentialSearchResult
    {
        public DonorResult Donor { get; set; }

        public int TotalMatchCount
        {
            get { return LocusMatchDetails.Where(m => m != null).Select(m => m.MatchCount).Sum(); }
        }

        private IEnumerable<LocusMatchDetails> LocusMatchDetails => new List<LocusMatchDetails>
        {
            MatchDetailsAtLocusA,
            MatchDetailsAtLocusB,
            MatchDetailsAtLocusC,
            MatchDetailsAtLocusDqb1,
            MatchDetailsAtLocusDrb1
        };

        public int TypedLociCount { get; set; }
        public int MatchRank { get; set; }
        public int TotalMatchGrade { get; set; }
        public int TotalMatchConfidence { get; set; }
        private LocusMatchDetails MatchDetailsAtLocusA { get; set; }
        private LocusMatchDetails MatchDetailsAtLocusB { get; set; }
        private LocusMatchDetails MatchDetailsAtLocusC { get; set; }
        private LocusMatchDetails MatchDetailsAtLocusDrb1 { get; set; }
        private LocusMatchDetails MatchDetailsAtLocusDqb1 { get; set; }

        public LocusMatchDetails MatchDetailsForLocus(Locus locus)
        {
            LocusMatchDetails matchDetails;
            switch (locus)
            {
                case Locus.A:
                    matchDetails = MatchDetailsAtLocusA;
                    break;
                case Locus.B:
                    matchDetails = MatchDetailsAtLocusB;
                    break;
                case Locus.C:
                    matchDetails = MatchDetailsAtLocusC;
                    break;
                case Locus.Dqb1:
                    matchDetails = MatchDetailsAtLocusDqb1;
                    break;
                case Locus.Drb1:
                    matchDetails = MatchDetailsAtLocusDrb1;
                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (matchDetails == null)
            {
                throw new Exception($"Attempted to access match details for locus {locus} before they were generated");
            }

            return matchDetails;
        }
        
        public void SetMatchDetailsForLocus(Locus locus, LocusMatchDetails locusMatchDetails)
        {
            switch (locus)
            {
                case Locus.A:
                    MatchDetailsAtLocusA = locusMatchDetails;
                    break;
                case Locus.B:
                    MatchDetailsAtLocusB = locusMatchDetails;
                    break;
                case Locus.C:
                    MatchDetailsAtLocusC = locusMatchDetails;
                    break;
                case Locus.Dqb1:
                    MatchDetailsAtLocusDqb1 = locusMatchDetails;
                    break;
                case Locus.Drb1:
                    MatchDetailsAtLocusDrb1 = locusMatchDetails;
                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}