using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Repositories.Hla
{
    /// <summary>
    /// An in-memory implementation of the matching dictionary lookup, for testing.
    /// </summary>
    public class HlaRepository : IMatchingDictionaryLookupService
    {
        // TODO:NOVA-928 this is a temporary in-memory solution based on a static file.
        // We will need to be able to regenerate the dictionary when needed, whether it remains as a file or moves into a DB.
        private readonly IEnumerable<RawMatchingHla> rawMatchingData = ReadJsonFromFile();

        private static IEnumerable<RawMatchingHla> ReadJsonFromFile()
        {
            System.Reflection.Assembly assem = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assem.GetManifestResourceStream("Nova.SearchAlgorithm.Resources.matching_hla.json"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<IEnumerable<RawMatchingHla>> (reader.ReadToEnd());
                }
            }
        }

        public Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName)
        {
            var raw = hlaName == null
                ? Enumerable.Empty<RawMatchingHla>()
                : rawMatchingData.Where(hla => hla.Locus.Equals(matchLocus.ToString(), StringComparison.InvariantCultureIgnoreCase) && hla.Name.StartsWith(hlaName));

            return Task.FromResult<IMatchingHlaLookupResult>(new MatchingDictionaryEntry(
                matchLocus,
                hlaName,
                TypingMethod.Molecular, // not used
                MolecularSubtype.CompleteAllele, // not used
                SerologySubtype.Associated, // not used
                raw.SelectMany(r => r.MatchingPGroups).Distinct(),
                null,
                null));
        }
    }
}
