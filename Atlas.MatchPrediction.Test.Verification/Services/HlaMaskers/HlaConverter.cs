using System;
using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models.FileSchema;
using MoreLinq.Extensions;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    public enum HlaConversionCategory
    {
        PGroup,
        Serology
    }

    internal interface IHlaConverter
    {
        /// <summary>
        /// Randomly selects typing that the HLA converts to, else returns the original HLA if conversion not possible.
        ///
        /// E.g., If <param name="hlaConversionCategory"/> is Serology, and allele has multiple serologies: returns a random serology.
        /// E.g., If <param name="hlaConversionCategory"/> is PGroup or Serology, and allele is non-expressing: returns the original allele.
        /// E.g., If <param name="hlaConversionCategory"/> is Serology, but the expressing allele has no known equivalent serology: returns the original allele.
        
        /// Only a limited number of HLA conversion categories are supported at present;
        /// exception will be thrown if any other category is requested.
        /// </summary>
        Task<TransformationResult> ConvertRandomLocusHla(
            TransformationRequest request,
            string hlaNomenclatureVersion,
            ImportTypingCategory inputCategory,
            HlaConversionCategory hlaConversionCategory);
    }

    internal class HlaConverter : HlaTransformerBase, IHlaConverter
    {
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public HlaConverter(IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        public async Task<TransformationResult> ConvertRandomLocusHla(
            TransformationRequest request,
            string hlaNomenclatureVersion,
            ImportTypingCategory inputCategory,
            HlaConversionCategory hlaConversionCategory)
        {
            if (request.Typings.IsNullOrEmpty())
            {
                return new TransformationResult();
            }

            return await TransformRandomlySelectedTypings(
                request,
                async hla => await TransformHla(request.Locus, hla, hlaNomenclatureVersion, inputCategory, hlaConversionCategory));
        }

        private async Task<string> TransformHla(
            Locus locus,
            string hla,
            string hlaNomenclatureVersion,
            ImportTypingCategory inputCategory,
            HlaConversionCategory hlaConversionCategory)
        {
            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var convertedHla = inputCategory switch
            {
                ImportTypingCategory.LargeGGroup => await TransformLargeGGroup(hlaMetadataDictionary, locus, hla, hlaConversionCategory),
                ImportTypingCategory.SmallGGroup => await TransformSmallGGroup(hlaMetadataDictionary, locus, hla, hlaConversionCategory),
                _ => throw new ArgumentOutOfRangeException(nameof(inputCategory), inputCategory, null)
            };

            return convertedHla.Shuffle().FirstOrDefault() ?? hla;
        }

        private static async Task<IEnumerable<string>> TransformLargeGGroup(
            IHlaMetadataDictionary hlaMetadataDictionary,
            Locus locus,
            string hla,
            HlaConversionCategory hlaConversionCategory)
        {
            return await hlaMetadataDictionary.ConvertHla(locus, hla, hlaConversionCategory.ToTargetHlaCategory());
        }

        private async Task<IEnumerable<string>> TransformSmallGGroup(
        IHlaMetadataDictionary hlaMetadataDictionary,
        Locus locus,
        string hla,
        HlaConversionCategory hlaConversionCategory)
        {
            // HMD can currently only convert a small g typing to P Group.
            // This is sufficient for transformation to the only supported categories of P Group or Serology.
            var pGroup = await hlaMetadataDictionary.ConvertSmallGGroupToPGroup(locus, hla);

            if (pGroup.IsNullOrEmpty())
            {
                return new List<string>();
            }

            return hlaConversionCategory switch
            {
                HlaConversionCategory.PGroup => new[] { pGroup },
                HlaConversionCategory.Serology => await hlaMetadataDictionary.ConvertHla(locus, pGroup, hlaConversionCategory.ToTargetHlaCategory()),
                _ => throw new ArgumentOutOfRangeException(nameof(hlaConversionCategory), hlaConversionCategory, null)
            };
        }
    }

    internal static class Converters
    {
        internal static TargetHlaCategory ToTargetHlaCategory(this HlaConversionCategory hlaConversionCategory) => hlaConversionCategory switch
        {
            HlaConversionCategory.PGroup => TargetHlaCategory.PGroup,
            HlaConversionCategory.Serology => TargetHlaCategory.Serology,
            _ => throw new ArgumentOutOfRangeException(nameof(hlaConversionCategory))
        };
    }
}