using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.HlaDataConversion
{
    internal interface IMatchedHlaDataConverterBase
    {
        IEnumerable<ISerialisableHlaMetadata> ConvertToHlaMetadata(IEnumerable<IMatchedHla> matchedHla);
    }

    /// <summary>
    /// Converts matched HLA to models that are optimised for HLA lookups.
    /// </summary>
    internal abstract class MatchedHlaDataConverterBase : IMatchedHlaDataConverterBase
    {
        public IEnumerable<ISerialisableHlaMetadata> ConvertToHlaMetadata(IEnumerable<IMatchedHla> matchedHla)
        {
            var matchedHlaList = matchedHla.ToList();

            return
                GetHlaMetadataFromMatchedSerologies(
                        matchedHlaList.OfType<IHlaMetadataSource<SerologyTyping>>())
                    .Concat(GetHlaMetadataFromMatchedAlleles(
                        matchedHlaList.OfType<IHlaMetadataSource<AlleleTyping>>()));
        }

        private IEnumerable<ISerialisableHlaMetadata> GetHlaMetadataFromMatchedSerologies(
            IEnumerable<IHlaMetadataSource<SerologyTyping>> matchedSerologies)
        {
            return matchedSerologies.Select(GetSerologyMetadata);
        }

        private IEnumerable<ISerialisableHlaMetadata> GetHlaMetadataFromMatchedAlleles(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> matchedAlleles)
        {
            var singleAlleleLookupSource = matchedAlleles.ToList();
            var alleleStringLookupSource = MultipleAlleleNullFilter.Filter(singleAlleleLookupSource).ToList();

            return
                GetMetadataForSingleAlleles(singleAlleleLookupSource)
                    .Concat(GetMetadataForNmdpCodeAlleleNames(alleleStringLookupSource))
                    .Concat(GetMetadataForXxCodeNames(alleleStringLookupSource));
        }

        /// <summary>
        /// Maps data using original allele names with no modification.
        /// </summary>
        private IEnumerable<ISerialisableHlaMetadata> GetMetadataForSingleAlleles(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> matchedAlleles)
        {
            return matchedAlleles.Select(GetSingleAlleleMetadata);
        }

        /// <summary>
        /// Coallesces data for alleles with 3+ fields that share the same locus and two-field name
        /// to speed up NMDP code lookups.
        /// </summary>
        private IEnumerable<ISerialisableHlaMetadata> GetMetadataForNmdpCodeAlleleNames(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> matchedAlleles)
        {
            var allelesGroupedByLocusAndLookupName = matchedAlleles
                // We must use names both with and without the expression suffix. This is because truncated allele names with a null suffix mean a different thing than those without:
                // e.g. 01:01 can refer to all 3/4 field alleles starting with 01:01, 01:01N refers only to the null alleles in this group
                // Both can be used for lookup, so we must treat then independently
                .SelectMany(allele => allele.TypingForHlaMetadata.ToNmdpCodeAlleleLookupNames(),
                    (allele, nmdpLookupName) => new {allele, nmdpLookupName})
                .Where(x => x.allele.TypingForHlaMetadata.Fields.Count() > 2)
                .GroupBy(x => new
                {
                    x.allele.TypingForHlaMetadata.Locus,
                    x.nmdpLookupName
                }, t => t.allele);

            return allelesGroupedByLocusAndLookupName
                .Select(x => GetNmdpCodeAlleleMetadata(x, x.Key.nmdpLookupName));
        }

        /// <summary>
        /// Coalesces data for alleles with 2+ fields that share the same locus and first field
        /// to speed up XX code lookups.
        /// </summary>
        private IEnumerable<ISerialisableHlaMetadata> GetMetadataForXxCodeNames(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> matchedAlleles)
        {
            // A few deleted alleles in hla_nom do not conform to v3.0 HLA nomenclature standards: 
            // they lack field delimiters and will be assigned a field count of 1.
            // These alleles must be excluded from the lookup results.

            var allelesGroupedByLocusAndLookupName = matchedAlleles
                .Where(matchedAllele => matchedAllele.TypingForHlaMetadata.Fields.Count() > 1)
                .GroupBy(matchedAllele => new
                {
                    matchedAllele.TypingForHlaMetadata.Locus,
                    LookupName = matchedAllele.TypingForHlaMetadata.ToXxCodeLookupName()
                });

            return allelesGroupedByLocusAndLookupName
                .Select(GetXxCodeMetadata);
        }

        #region Abstract methods

        /// <summary>
        /// Maps data using original serology name with no modification.
        /// </summary>
        protected abstract ISerialisableHlaMetadata GetSerologyMetadata(
            IHlaMetadataSource<SerologyTyping> metadataSource);

        /// <summary>
        /// Maps data using original allele name with no modification.
        /// </summary>
        protected abstract ISerialisableHlaMetadata GetSingleAlleleMetadata(
            IHlaMetadataSource<AlleleTyping> metadataSource);

        /// <summary>
        /// To create lookup result for an NMDP code allele, pass in a set of allele typings 
        /// that map to the same Locus & NMDP code allele lookup name value.
        /// </summary>
        protected abstract ISerialisableHlaMetadata GetNmdpCodeAlleleMetadata(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> metadataSources,
            string nmdpLookupName);

        /// <summary>
        /// To create an XX code lookup result, pass in a set of allele typings 
        /// that map to the same Locus & XX code lookup name value.
        /// </summary>
        protected abstract ISerialisableHlaMetadata GetXxCodeMetadata(
            IEnumerable<IHlaMetadataSource<AlleleTyping>> metadataSources);

        #endregion
    }
}