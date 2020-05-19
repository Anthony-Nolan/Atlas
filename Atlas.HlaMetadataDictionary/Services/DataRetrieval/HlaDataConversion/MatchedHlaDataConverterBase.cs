using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.HlaDataConversion
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
            var alleleStringLookupSource = MultipleAlleleNullFilter.Filter(singleAlleleLookupSource).ToList();

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
            return matchedAlleles.Select(GetSingleAlleleLookupResult);
        }

        /// <summary>
        /// Coallesces data for alleles with 3+ fields that share the same locus and two-field name
        /// to speed up NMDP code lookups.
        /// </summary>
        private IEnumerable<IHlaLookupResult> GetLookupResultsForNmdpCodeAlleleNames(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> matchedAlleles)
        {
            var allelesGroupedByLocusAndLookupName = matchedAlleles
                // We must use names both with and without the expression suffix. This is because truncated allele names with a null suffix mean a different thing than those without:
                // e.g. 01:01 can refer to all 3/4 field alleles starting with 01:01, 01:01N refers only to the null alleles in this group
                // Both can be used for lookup, so we must treat then independently
                .SelectMany(allele => allele.TypingForHlaLookupResult.ToNmdpCodeAlleleLookupNames(),
                    (allele, nmdpLookupName) => new {allele, nmdpLookupName})
                .Where(x => x.allele.TypingForHlaLookupResult.Fields.Count() > 2)
                .GroupBy(x => new
                {
                    x.allele.TypingForHlaLookupResult.Locus,
                    x.nmdpLookupName
                }, t => t.allele);

            return allelesGroupedByLocusAndLookupName
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

            var allelesGroupedByLocusAndLookupName = matchedAlleles
                .Where(matchedAllele => matchedAllele.TypingForHlaLookupResult.Fields.Count() > 1)
                .GroupBy(matchedAllele => new
                {
                    matchedAllele.TypingForHlaLookupResult.Locus,
                    LookupName = matchedAllele.TypingForHlaLookupResult.ToXxCodeLookupName()
                });

            return allelesGroupedByLocusAndLookupName
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
        /// that map to the same Locus & NMDP code allele lookup name value.
        /// </summary>
        protected abstract IHlaLookupResult GetNmdpCodeAlleleLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources,
            string nmdpLookupName);

        /// <summary>
        /// To create an XX code lookup result, pass in a set of allele typings 
        /// that map to the same Locus & XX code lookup name value.
        /// </summary>
        protected abstract IHlaLookupResult GetXxCodeLookupResult(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources);

        #endregion
    }
}