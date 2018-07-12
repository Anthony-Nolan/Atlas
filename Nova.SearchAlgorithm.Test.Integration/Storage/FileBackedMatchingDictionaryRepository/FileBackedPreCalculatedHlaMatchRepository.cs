using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedMatchingDictionaryRepository
{
    /// <summary>
    /// An implementation of the matching dictionary lookup which loads the data from a file,
    /// necessary for testing without an internet dependency.
    /// </summary>
    public class FileBackedPreCalculatedHlaMatchRepository : IPreCalculatedHlaMatchRepository
    {
        private readonly IEnumerable<RawMatchingHla> rawMatchingData = ReadJsonFromFile();

        private static IEnumerable<RawMatchingHla> ReadJsonFromFile()
        {
            System.Reflection.Assembly assem = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assem.GetManifestResourceStream("Nova.SearchAlgorithm.Test.Integration.Resources.MatchingDictionary.matching_hla.json"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<IEnumerable<RawMatchingHla>> (reader.ReadToEnd());
                }
            }
        }

        public Task RecreatePreCalculatedHlaMatchesTable(IEnumerable<PreCalculatedHlaMatchInfo> dictionaryContents)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task<PreCalculatedHlaMatchInfo> GetPreCalculatedHlaMatchInfoIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var raw = rawMatchingData.FirstOrDefault(
                    hla => hla.MatchLocus.Equals(matchLocus.ToString(), StringComparison.InvariantCultureIgnoreCase) 
                           && hla.LookupName == lookupName);

            if (raw == null)
            {
                return null;
            }

            var lookupResult = new PreCalculatedHlaMatchInfo(
                matchLocus,
                raw.LookupName,
                TypingMethod.Molecular, // Arbitrary, not used in tests
                MolecularSubtype.CompleteAllele, // Arbitrary, not used in tests
                SerologySubtype.Associated, // Arbitrary, not used in tests
                AlleleTypingStatus.GetDefaultStatus(), // Default, not used in tests
                raw.MatchingPGroups,
                raw.MatchingGGroups,
                new List<SerologyEntry>() // Empty, not used in tests
                );
            
            return Task.FromResult(lookupResult);
        }

        public Task LoadPreCalculatedHlaMatchesIntoMemory()
        {
            return Task.CompletedTask;
        }

        public IEnumerable<string> GetAllPGroups()
        {
            return rawMatchingData.SelectMany(hla => hla.MatchingPGroups);
        }
    }
}
