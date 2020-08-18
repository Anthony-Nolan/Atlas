using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Castle.Core.Internal;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq.Extensions;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal interface IHlaConverter
    {
        Task<TransformationResult> ConvertRandomLocusHla(TransformationRequest request, string hlaNomenclatureVersion, TargetHlaCategory target);
    }

    internal class HlaConverter : HlaTransformerBase, IHlaConverter
    {
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public HlaConverter(IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        /// <returns>Randomly selected target typing that the HLA converts to, else returns the original HLA if conversion not possible.
        /// E.g., If <param name="target"/> is Serology, and allele has multiple serologies: returns a random serology.
        /// E.g., If <param name="target"/> is PGroup or Serology, and allele is non-expressing: returns the original allele.
        /// E.g., If <param name="target"/> is Serology, but the expressing allele has no known equivalent serology: returns the original allele.
        /// </returns>
        public async Task<TransformationResult> ConvertRandomLocusHla(TransformationRequest request,
            string hlaNomenclatureVersion,
            TargetHlaCategory target)
        {
            if (request.Typings.IsNullOrEmpty())
            {
                return new TransformationResult();
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            return await TransformRandomlySelectedTypings(
                request,
                async hla => (await hlaMetadataDictionary.ConvertHla(request.Locus, hla, target)).Shuffle().FirstOrDefault() ?? hla);
        }
    }
}