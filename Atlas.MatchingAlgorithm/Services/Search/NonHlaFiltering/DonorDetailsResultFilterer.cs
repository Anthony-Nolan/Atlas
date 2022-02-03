using System.Collections.Generic;
using System.Linq;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Services.Search.NonHlaFiltering
{
    internal class DonorFilteringCriteria
    {
        public List<string> RegistryCodes { get; set; }
    }
    
    internal interface IDonorDetailsResultFilterer
    {
        /// <param name="donorFilteringCriteria"></param>
        /// <param name="results"></param>
        /// <param name="donorLookup">Must contain a lookup for all donors included in <see cref="results"/> - if any are missing, exception will be thrown.</param>
        IEnumerable<MatchAndScoreResult> FilterResultsByDonorData(
            DonorFilteringCriteria donorFilteringCriteria,
            IEnumerable<MatchAndScoreResult> results,
            IReadOnlyDictionary<int, Donor> donorLookup);
    }

    internal class DonorDetailsResultFilterer : IDonorDetailsResultFilterer
    {
        /// <inheritdoc />
        public IEnumerable<MatchAndScoreResult> FilterResultsByDonorData(
            DonorFilteringCriteria donorFilteringCriteria,
            IEnumerable<MatchAndScoreResult> results,
            IReadOnlyDictionary<int, Donor> donorLookup)
        {
            if (donorFilteringCriteria.RegistryCodes != null)
            {
                results = results.Where(r =>
                {
                    var donor = donorLookup[r.MatchResult.DonorId];
                    return donorFilteringCriteria.RegistryCodes.Contains(donor.RegistryCode);
                });
            }

            return results;
        }
    }
}