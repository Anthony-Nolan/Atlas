using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.Test.Integration.FileBackedMatchingDictionary
{
    /// <summary>
    /// An implementation of the matching dictionary lookup which loads the data from a file,
    /// necessary for testing without an internet dependency.
    /// </summary>
    public class FileBackedMatchingDictionaryRepository : IMatchingDictionaryRepository
    {
        private readonly IEnumerable<RawMatchingHla> rawMatchingData = ReadJsonFromFile();

        private static IEnumerable<RawMatchingHla> ReadJsonFromFile()
        {
            System.Reflection.Assembly assem = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assem.GetManifestResourceStream("Nova.SearchAlgorithm.Test.Resources.MatchingDictionary.matching_hla.json"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<IEnumerable<RawMatchingHla>> (reader.ReadToEnd());
                }
            }
        }

        public Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var raw = rawMatchingData.FirstOrDefault(
                    hla => hla.MatchLocus.Equals(matchLocus.ToString(), StringComparison.InvariantCultureIgnoreCase) 
                           && hla.LookupName == lookupName);

            if (raw == null)
            {
                return null;
            }

            var lookupResult = new MatchingDictionaryEntry(
                matchLocus,
                raw.LookupName,
                TypingMethod.Molecular, // Arbitrary, not used in tests
                MolecularSubtype.CompleteAllele, // Arbitrary, not used in tests
                SerologySubtype.Associated, // Arbitrary, not used in tests
                raw.MatchingPGroups,
                raw.MatchingGGroups,
                new List<SerologyEntry>() // Empty, not used in tests
                );
            
            return Task.FromResult(lookupResult);
        }
    }
}
