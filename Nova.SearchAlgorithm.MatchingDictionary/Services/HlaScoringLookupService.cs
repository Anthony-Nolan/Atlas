using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Determines and executes the dictionary lookup strategy for submitted HLA types.
    /// </summary>
    public interface IHlaScoringLookupService
    {
        /// <summary>
        ///  Expands the hla name into a list of HLA scoring lookup results.
        /// </summary>
        /// <returns>A HLA Scoring Lookup Result for each HLA typing that maps to the HLA name.</returns>
        Task<IHlaScoringLookupResult> GetHlaScoringLookupResults(MatchLocus matchLocus, string hlaName);
    }

    public class HlaScoringLookupService : 
        LookupServiceBase<IHlaScoringLookupResult>, IHlaScoringLookupService
    {
        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResults(MatchLocus matchLocus, string hlaName)
        {
            // TODO: NOVA-1445: lookup should return a list of non-consolidated entries
            throw new System.NotImplementedException();
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName);
        }

        protected override Task<IHlaScoringLookupResult> PerformLookup(MatchLocus matchLocus, string lookupName)
        {
            // TODO: NOVA-1445: lookup should return a list of non-consolidated entries
            throw new System.NotImplementedException();
        }
    }
}