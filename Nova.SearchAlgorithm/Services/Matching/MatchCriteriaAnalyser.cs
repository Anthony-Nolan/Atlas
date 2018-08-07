using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IMatchCriteriaAnalyser
    {
        /// <summary>
        /// Determines for which loci the matching database should be hit
        /// This takes into account mismatch counts - the database can only return results with at least one match, so it is pointless to query it for a locus with two allowed mismatches
        /// It may 
        /// </summary>
        IEnumerable<Locus> LociToMatchInDatabase(AlleleLevelMatchCriteria criteria);
    }

    public class MatchCriteriaAnalyser : IMatchCriteriaAnalyser
    {
        // TODO: NOVA-1395: Dynamically decide which loci to initially query for based on criteria, optimising for search speed
        public IEnumerable<Locus> LociToMatchInDatabase(AlleleLevelMatchCriteria criteria)
        {
            var lociToSearchInDatabase = new List<Locus>();
            
            // Prefer to avoid searching for Locus A as it is the largest dataset, and takes longest to query
            if (criteria.LocusMismatchB.MismatchCount < 2)
            {
                lociToSearchInDatabase.Add(Locus.B);
            }

            if (criteria.LocusMismatchDRB1.MismatchCount < 2)
            {
                lociToSearchInDatabase.Add(Locus.Drb1);
            }

            // Searching on two loci necessary to narrow down results enough to be suitably performant
            if (lociToSearchInDatabase.Count() < 2 && criteria.LocusMismatchA.MismatchCount < 2)
            {
                lociToSearchInDatabase.Add(Locus.A);
            }

            if (!lociToSearchInDatabase.Any())
            {
                throw new NotImplementedException("The facility does not yet exist to run a search with 2 allowed mismatches at A, B and DRB1. Please refine the search criteria");
            }

            return lociToSearchInDatabase;
        }
    }
}