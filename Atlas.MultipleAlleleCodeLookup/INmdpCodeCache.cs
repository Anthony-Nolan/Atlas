using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;

namespace Atlas.MultipleAlleleCodeDictionary
{
    /// <summary>
    /// Other packages need to consume a MAC cache populated elsewhere.
    /// The cache should be exposed via this interface to ensure lazy re-population of an expired cache
    /// </summary>
    public interface INmdpCodeCache
    {
        Task<IEnumerable<string>> GetOrAddAllelesForNmdpCode(Locus locus, string nmdpCode);
    }
}