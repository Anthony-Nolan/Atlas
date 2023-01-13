using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using MoreLinq.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal interface IMacBuilder
    {
        Task<TransformationResult> ConvertRandomHlaToMacs(TransformationRequest request, string hlaNomenclatureVersion);
    }

    internal class MacBuilder : HlaTransformerBase, IMacBuilder
    {
        private readonly IXxCodeBuilder xxCodeBuilder;
        private readonly IHlaMetadataDictionaryFactory hmdFactory;
        private readonly IExpandedMacCache cache;

        public MacBuilder(
            IXxCodeBuilder xxCodeBuilder,
            IHlaMetadataDictionaryFactory hmdFactory,
            IExpandedMacCache cache)
        {
            this.xxCodeBuilder = xxCodeBuilder;
            this.hmdFactory = hmdFactory;
            this.cache = cache;
        }

        public async Task<TransformationResult> ConvertRandomHlaToMacs(TransformationRequest request, string hlaNomenclatureVersion)
        {
            if (request.Typings.IsNullOrEmpty())
            {
                return new TransformationResult();
            }

            var hmd = hmdFactory.BuildDictionary(hlaNomenclatureVersion);
            return await TransformRandomlySelectedTypings(request, hla => BuildMac(request.Locus, hla, hmd));
        }

        /// <returns>Generic MAC whose expanded definition covers at least one allele within the G group
        /// submitted in <paramref name="hlaName"/> AND a subset of its related alleles
        /// (i.e., those sharing the same locus and first field).
        /// Returns <paramref name="hlaName"/>, if no such code can be found.
        /// </returns>
        private async Task<string> BuildMac(Locus locus, string hlaName, IHlaMetadataDictionary hmd)
        {
            var relatedSecondFields = await GetSecondFieldsOfRelatedAlleles(locus, hlaName, hmd);
            var potentialMacs = await cache.GetCodesBySecondField(AlleleSplitter.SecondFieldWithSuffixRemoved(hlaName));

            foreach (var mac in potentialMacs.Shuffle())
            {
                var macSecondFields = Enumerable.ToHashSet(await cache.GetSecondFieldsByCode(mac));

                if (macSecondFields.IsSubsetOf(relatedSecondFields))
                {
                    return $"{AlleleSplitter.FirstField(hlaName)}:{mac}";
                }
            }

            return hlaName;
        }

        private async Task<HashSet<string>> GetSecondFieldsOfRelatedAlleles(Locus locus, string hlaName, IHlaMetadataDictionary hmd)
        {
            var xxCode = xxCodeBuilder.ConvertHlaToXxCode(hlaName);

            // Currently Atlas has no neat way of directly expanding an XX code to 2 field alleles.
            // This is an indirect method: convert the XX code to all possible G groups using the HMD,
            // and then use the HMD again to further expand the returned G groups to 2 field alleles.
            var gGroups = await hmd.ConvertHla(locus, xxCode, TargetHlaCategory.GGroup);
            var alleles = await Task.WhenAll(gGroups.Select(async gGroup =>
                await hmd.ConvertHla(locus, gGroup, TargetHlaCategory.TwoFieldAlleleExcludingExpressionSuffix)));

            return Enumerable.ToHashSet(alleles.SelectMany(a => a)
                .Select(AlleleSplitter.SecondFieldWithSuffixRemoved)
                .Distinct()
                .OrderBy(x => x));
        }
    }
}