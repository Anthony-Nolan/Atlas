using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.Utils.Models;

namespace Atlas.MultipleAlleleCodeDictionary
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