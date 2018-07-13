using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedMatchingDictionaryRepository
{
    /// <summary>
    /// An implementation of the matching dictionary lookup which loads the data from a file,
    /// necessary for testing without an internet dependency.
    /// </summary>
    public class FileBackedHlaMatchingLookupRepository : IHlaMatchingLookupRepository
    {
        private readonly IEnumerable<RawMatchingHla> rawMatchingData = ReadJsonFromFile();

        private static IEnumerable<RawMatchingHla> ReadJsonFromFile()
        {
            System.Reflection.Assembly assem = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assem.GetManifestResourceStream("Nova.SearchAlgorithm.Test.Integration.Resources.MatchingDictionary.matching_hla.json"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<IEnumerable<RawMatchingHla>>(reader.ReadToEnd());
                }
            }
        }

        public Task RecreateDataTable(IEnumerable<IHlaLookupResult> dictionaryContents)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task LoadDataIntoMemory()
        {
            return Task.CompletedTask;
        }

        public Task<HlaMatchingLookupResult> GetHlaMatchLookupResultIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var raw = rawMatchingData.FirstOrDefault(
                    hla => hla.MatchLocus.Equals(matchLocus.ToString(), StringComparison.InvariantCultureIgnoreCase)
                           && hla.LookupName == lookupName);

            if (raw == null)
            {
                return null;
            }

            var lookupResult = new HlaMatchingLookupResult(
                matchLocus,
                raw.LookupName,
                TypingMethod.Molecular, // Arbitrary, not used in tests
                raw.MatchingPGroups
                );

            return Task.FromResult(lookupResult);
        }

        public IEnumerable<string> GetAllPGroups()
        {
            return rawMatchingData.SelectMany(hla => hla.MatchingPGroups);
        }
    }
}
