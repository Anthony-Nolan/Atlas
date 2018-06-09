using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class AlleleStringLookup : MultipleAllelesLookup
    {
        public AlleleStringLookup(IMatchingDictionaryRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        protected override Task<IEnumerable<string>> GetAlleles(MatchLocus matchLocus, string lookupName)
        {
            const char fieldDelimiter = ':';

            var splitLookupName = lookupName.Split('/').ToList();
            var firstAllele = splitLookupName[0];
            var familyFromFirstAllele = firstAllele.Split(fieldDelimiter)[0];
            var secondValue = splitLookupName[1];

            var alleles = secondValue.Contains(fieldDelimiter)
                ? splitLookupName
                : new List<string> { firstAllele }.Union(
                    splitLookupName.Skip(1).Select(subtype => familyFromFirstAllele + fieldDelimiter + subtype));

            return Task.FromResult(alleles);
        }
    }
}
