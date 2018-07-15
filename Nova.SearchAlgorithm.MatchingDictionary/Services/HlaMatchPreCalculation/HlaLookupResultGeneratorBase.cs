using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    public interface IHlaLookupResultGenerator
    {
        IEnumerable<IHlaLookupResult> GetHlaMatchingLookupResults(IEnumerable<IMatchedHla> matchedHla);
    }

    /// <summary>
    /// Optimises data in matched HLA objects for HLA matching lookups.
    /// </summary>
    public abstract class HlaLookupResultGeneratorBase<TLookupResult> : IHlaLookupResultGenerator
        where TLookupResult : IHlaLookupResult
    {
        public IEnumerable<IHlaLookupResult> GetHlaMatchingLookupResults(IEnumerable<IMatchedHla> matchedHla)
        {
            var matchedHlaList = matchedHla.ToList();

            var entries = new List<IHlaLookupResult>();
            entries.AddRange(GetHlaLookupResultsFromMatchedAlleles(matchedHlaList.OfType<IHlaLookupResultSource<AlleleTyping>>()));
            entries.AddRange(GetHlaLookupResultsFromMatchedSerologies(matchedHlaList.OfType<IHlaLookupResultSource<SerologyTyping>>()));

            return entries;
        }

        private IEnumerable<IHlaLookupResult> GetHlaLookupResultsFromMatchedAlleles(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> matchedAlleles)
        {
            var entries = matchedAlleles.SelectMany(GetHlaMatchingLookupResultsForEachAlleleLookupName);

            return GroupResultsToMergeDuplicatesCausedByAlleleNameTruncation(entries);
        }

        protected abstract IEnumerable<IHlaLookupResult> GetHlaLookupResultsFromMatchedSerologies(
            IEnumerable<IHlaLookupResultSource<SerologyTyping>> matchedSerology);

        protected abstract IEnumerable<TLookupResult> GetHlaMatchingLookupResultsForEachAlleleLookupName(
            IHlaLookupResultSource<AlleleTyping> matchedAllele);

        protected abstract IEnumerable<IHlaLookupResult> GroupResultsToMergeDuplicatesCausedByAlleleNameTruncation(
            IEnumerable<TLookupResult> lookupResults);

        protected static IEnumerable<string> GetAlleleLookupNames(AlleleTyping alleleTyping)
        {
            return new List<string>
            {
                alleleTyping.Name,
                alleleTyping.TwoFieldName,
                alleleTyping.Fields.ElementAt(0)
            };
        }
    }
}
