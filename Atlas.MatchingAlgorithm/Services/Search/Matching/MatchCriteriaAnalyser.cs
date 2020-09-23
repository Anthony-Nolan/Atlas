using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IMatchCriteriaAnalyser
    {
        /// <summary>
        /// Determines for which loci the matching database should be hit
        /// This takes into account mismatch counts - the database can only return results with at least one match, so it is pointless to query it for a locus with two allowed mismatches
        /// It may 
        /// </summary>
        IEnumerable<Locus> LociToMatchFirst(AlleleLevelMatchCriteria criteria);
    }

    public class MatchCriteriaAnalyser : IMatchCriteriaAnalyser
    {
        // TODO: NOVA-1395: Dynamically decide which loci to initially query for based on criteria, optimising for search speed
        public IEnumerable<Locus> LociToMatchFirst(AlleleLevelMatchCriteria criteria)
        {
            var lociToSearchInDatabase = new List<Locus>();

            // Prefer to avoid searching for Locus A as it is the largest dataset, and takes longest to query
            if (criteria.LocusCriteria.B.MismatchCount == 0)
            {
                lociToSearchInDatabase.Add(Locus.B);
            }

            if (criteria.LocusCriteria.Drb1.MismatchCount == 0)
            {
                lociToSearchInDatabase.Add(Locus.Drb1);
            }

            // Searching on two loci necessary to narrow down results enough to be suitably performant
            if (lociToSearchInDatabase.Count() < 2 && criteria.LocusCriteria.A.MismatchCount < 2)
            {
                lociToSearchInDatabase.Add(Locus.A);
            }

            if (!lociToSearchInDatabase.Contains(Locus.B) && lociToSearchInDatabase.Count() < 2 && criteria.LocusCriteria.B.MismatchCount < 2)
            {
                lociToSearchInDatabase.Add(Locus.B);
            }

            if (!lociToSearchInDatabase.Contains(Locus.Drb1) && lociToSearchInDatabase.Count() < 2 && criteria.LocusCriteria.Drb1.MismatchCount < 2)
            {
                lociToSearchInDatabase.Add(Locus.Drb1);
            }
            
            if (!lociToSearchInDatabase.Any())
            {
                // Unless we allow 6+ mismatches total across A, B, DRB1, at least one of them must have at least one match
                if (criteria.DonorMismatchCount < 6)
                {
                    lociToSearchInDatabase = LocusSettings.LociPossibleToMatchInMatchingPhaseOne.ToList();
                }
                else
                {
                    throw new NotImplementedException("A search cannot be run with 2 allowed mismatches at A, B and DRB1 at 6+ total mismatches. Please refine the search criteria");
                }
            }

            return lociToSearchInDatabase;
        }
    }
}