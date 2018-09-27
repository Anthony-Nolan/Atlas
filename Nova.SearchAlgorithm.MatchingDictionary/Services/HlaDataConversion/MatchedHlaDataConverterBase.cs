using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Properties;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion
{
    public interface IMatchedHlaDataConverterBase
    {
        IEnumerable<IHlaLookupResult> ConvertToHlaLookupResults(IEnumerable<IMatchedHla> matchedHla);
    }

    /// <summary>
    /// Converts matched HLA to models that are optimised for HLA lookups.
    /// </summary>
    public abstract class MatchedHlaDataConverterBase : IMatchedHlaDataConverterBase
    {
        public IEnumerable<IHlaLookupResult> ConvertToHlaLookupResults(IEnumerable<IMatchedHla> matchedHla)
        {
            var matchedHlaList = matchedHla.ToList();

            return
                GetHlaLookupResultsFromMatchedSerologies(
                        matchedHlaList.OfType<IHlaLookupResultSource<SerologyTyping>>())
                    .Concat(GetHlaLookupResultsFromMatchedAlleles(
                        matchedHlaList.OfType<IHlaLookupResultSource<AlleleTyping>>()));
        }

        private IEnumerable<IHlaLookupResult> GetHlaLookupResultsFromMatchedSerologies(
            IEnumerable<IHlaLookupResultSource<SerologyTyping>> matchedSerologies)
        {
            return matchedSerologies.Select(GetSerologyLookupResult);
        }

        private IEnumerable<IHlaLookupResult> GetHlaLookupResultsFromMatchedAlleles(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> matchedAlleles)
        {
            var singleAlleleLookupSource = matchedAlleles.ToList();

            // If feature flag is turned on: 
            // For building single allele lookups, all alleles must be used.
            // For building allele string lookups, only expressing alleles should be used.
            var alleleStringLookupSource = StaticFeatureFlags.ShouldIgnoreNullAllelesInAlleleStrings
                ? singleAlleleLookupSource
                    .Where(allele => !allele.TypingForHlaLookupResult.IsNullExpresser)
                    .ToList()
                : singleAlleleLookupSource;

            return
                GetLookupResultsForSingleAlleles(singleAlleleLookupSource)
                    .Concat(GetLookupResultsForNmdpCodeAlleleNames(alleleStringLookupSource))
                    .Concat(GetLookupResultsForXxCodeNames(alleleStringLookupSource));
        }

        /// <summary>
        /// Maps data using original allele names with no modification.
        /// </summary>
        private IEnumerable<IHlaLookupResult> GetLookupResultsForSingleAlleles(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> matchedAlleles)
        {
            return matchedAlleles
                .Select(GetSingleAlleleLookupResult);
        }

        /// <summary>
        /// Coallesces data for alleles with 3+ fields that share the same locus and two-field name
        /// to speed up NMDP code lookups.
        /// </summary>
        private IEnumerable<IHlaLookupResult> GetLookupResultsForNmdpCodeAlleleNames(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> matchedAlleles)
        {
            var allelesGroupedByMatchLocusAndLookupName = matchedAlleles
                .SelectMany(allele => allele.TypingForHlaLookupResult.ToNmdpCodeAlleleLookupNames(),
                    (allele, nmdpLookupName) => new {allele, nmdpLookupName})
                .Where(x => x.allele.TypingForHlaLookupResult.Fields.Count() > 2)
                .GroupBy(x => new
                {
                    x.allele.TypingForHlaLookupResult.MatchLocus,
                    x.nmdpLookupName
                }, t => t.allele);

            return allelesGroupedByMatchLocusAndLookupName
                .Select(x => GetNmdpCodeAlleleLookupResult(x, x.Key.nmdpLookupName));
        }

        /// <summary>
        /// Coalesces data for alleles with 2+ fields that share the same locus and first field
        /// to speed up XX code lookups.
        /// </summary>
        private IEnumerable<IHlaLookupResult> GetLookupResultsForXxCodeNames(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> matchedAlleles)
        {
            // A few deleted alleles in hla_nom do not conform to v3.0 HLA nomenclature standards: 
            // they lack field delimiters and will be assigned a field count of 1.
            // These alleles must be excluded from the lookup results.

            var allelesGroupedByMatchLocusAndLookupName = matchedAlleles
                .Where(matchedAllele => matchedAllele.TypingForHlaLookupResult.Fields.Count() > 1)
                .GroupBy(matchedAllele => new
                {
                    matchedAllele.TypingForHlaLookupResult.MatchLocus,
                    LookupName = matchedAllele.TypingForHlaLookupResult.ToXxCodeLookupName()
                });

            return allelesGroupedByMatchLocusAndLookupName
                .Select(GetXxCodeLookupResult);
        }

        #region Abstract methods

        /// <summary>
        /// Maps data using original serology name with no modification.
        /// </summary>
        protected abstract IHlaLookupResult GetSerologyLookupResult(
            IHlaLookupResultSource<SerologyTyping> lookupResultSource);

        /// <summary>
        /// Maps data using original allele name with no modification.
        /// </summary>
        protected abstract IHlaLookupResult GetSingleAlleleLookupResult(
            IHlaLookupResultSource<AlleleTyping> lookupResultSource);

        /// <summary>
        /// To create lookup result for an NMDP code allele, pass in a set of allele typings 
        /// that map to the same MatchLocus & NMDP code allele lookup name value.
        /// </summary>
        protected abstract IHlaLookupResult GetNmdpCodeAlleleLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources,
            string nmdpLookupName);

        /// <summary>
        /// To create an XX code lookup result, pass in a set of allele typings 
        /// that map to the same MatchLocus & XX code lookup name value.
        /// </summary>
        protected abstract IHlaLookupResult GetXxCodeLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources);

        #endregion
    }
}