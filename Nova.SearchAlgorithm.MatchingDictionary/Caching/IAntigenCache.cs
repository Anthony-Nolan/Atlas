using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.MatchingDictionary.Caching
{
    /// <summary>
    /// The matching dictionary package needs to consume a cache populated elsewhere.
    /// The cache should be exposed via this interface to ensure lazy re-population of an expired cache
    /// </summary>
    public interface IAntigenCache
    {
        /// <returns>A lookup of NMDP codes to Full HLA Names</returns>
        Task<Dictionary<string, string>> GetNmdpCodeLookup(Locus locus);
    }
}