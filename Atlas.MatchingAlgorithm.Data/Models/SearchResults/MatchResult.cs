using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Data.Models.SearchResults
{
    public class MatchResult
    {
        public MatchResult(int donorId)
        {
            DonorId = donorId;
        }
        
        private DonorInfo.DonorInfo donorInfo;

        #region Partial donor information used in matching

        // Stored separately from the donors, as the lookup in the matches table only returns the id
        // We don't want to populate the full donor object until some filtering has been applied on those results
        public int DonorId { get; set; }

        #endregion

        public DonorInfo.DonorInfo DonorInfo
        {
            get
            {
                if (donorInfo == null)
                {
                    throw new Exception("Attempted to access expanded donor information before it was populated");
                }

                return donorInfo;
            }
            set
            {
                if (isMatchingDataFullyPopulated)
                {
                    throw new ReadOnlyException("Matching data cannot be changed after it has been marked as fully populated");
                }

                donorInfo = value;
            }
        }

        public int TotalMatchCount => MatchDetails.ToEnumerable().Where(m => m != null).Select(m => m.MatchCount).Sum();

        public int PopulatedLociCount => MatchDetails.ToEnumerable().Count(m => m != null);

        /// <summary>
        /// Returns the loci for which match results have been set
        /// </summary>
        internal IEnumerable<Locus> MatchedLoci => EnumerateValues<Locus>().Where(l => MatchDetailsForLocus(l) != null);

        // TODO: ATLAS-714: Can we do this without making it public? Need to allow for nulls for mismatches that weren't populated by later locus
        public LociInfo<LocusMatchDetails> MatchDetails { get; private set; } = new LociInfo<LocusMatchDetails>();

        // As this class is populated gradually over time, this is used to indicate when we've populated all matching data we plan to
        // Until then, accessing certain null values will throw exceptions, on the assumption they are not yet populated
        private bool isMatchingDataFullyPopulated;

        public LocusMatchDetails MatchDetailsForLocus(Locus locus)
        {
            var matchDetails = MatchDetails.GetLocus(locus);
            if (matchDetails == null && !isMatchingDataFullyPopulated)
            {
                // If not fully populated, consider the locus a mismatch. We can only assume that null = not matched once result is finalised. 
                // This case can occur in e.g. 8/10 searches, when a locus can be a full mismatch - donors that match at e.g. B but not A would have a null locus match details for A, as they were not matched during that locus' matching request. 
                return new LocusMatchDetails();
            }

            return matchDetails;
        }

        public void SetMatchDetailsForLocus(Locus locus, LocusMatchDetails locusMatchDetails)
        {
            if (isMatchingDataFullyPopulated)
            {
                throw new ReadOnlyException("Matching data cannot be changed after it has been marked as fully populated");
            }

            MatchDetails = MatchDetails.SetLocus(locus, locusMatchDetails);
        }

        public void MarkMatchingDataFullyPopulated() => isMatchingDataFullyPopulated = true;

        public int TypedLociCount => DonorInfo.HlaNames.Reduce((_, hla, count) => hla.Position1And2NotNull() ? count + 1 : count, 0);

        /// <summary>
        /// Ensures that any loci that were searched for, but have null values, are set to 0 match results, to distinguish them from un-searched loci.
        ///
        /// Converts nulls to empty search results at specified loci.
        /// </summary>
        /// <param name="loci">Searched loci.</param>
        public MatchResult PopulateMismatches(IEnumerable<Locus> loci)
        {
            foreach (var locus in loci)
            {
                if (MatchDetails.GetLocus(locus) == null)
                {
                    MatchDetails = MatchDetails.SetLocus(locus, new LocusMatchDetails());
                }
            }

            return this;
        }
    }
}