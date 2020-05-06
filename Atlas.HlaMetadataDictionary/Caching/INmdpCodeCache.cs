using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Caching
{
    /// <summary>
    /// The matching dictionary package needs to consume a cache populated elsewhere.
    /// The cache should be exposed via this interface to ensure lazy re-population of an expired cache
    /// </summary>
    public interface INmdpCodeCache
    {
        Task<IEnumerable<string>> GetOrAddAllelesForNmdpCode(Locus locus, string nmdpCode);
    }
}