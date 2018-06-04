using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using System.Threading.Tasks;
using NSubstitute;

namespace Nova.SearchAlgorithm.Test.FileBackedMatchingDictionary
{
    /// <summary>
    /// An implementation of the matching dictionary lookup which loads the data from a file,
    /// necessary for testing without an internet dependency.
    /// </summary>
    public class FileBackedMatchingDictionaryLookup : IMatchingDictionaryLookupService
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

        public Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName)
        {
            var raw = hlaName == null
                ? Enumerable.Empty<RawMatchingHla>()
                : rawMatchingData.Where(hla => hla.Locus.Equals(matchLocus.ToString(), StringComparison.InvariantCultureIgnoreCase) && hla.Name.StartsWith(hlaName));

            var lookupResult = Substitute.For<IMatchingHlaLookupResult>();
            lookupResult.MatchLocus.Returns(matchLocus);
            lookupResult.LookupName.Returns(hlaName);
            lookupResult.MatchingPGroups.Returns(raw.SelectMany(r => r.MatchingPGroups).Distinct());

            return Task.FromResult(lookupResult);
        }
    }
}
