using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Castle.Core.Internal;
using System.Linq;
using System.Threading.Tasks;

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

        /// <returns>Selects the first target typing the HLA converts to, else returns the unmodified HLA if it cannot be converted.
        /// E.g., An allele has multiple serologies, only the first serology is selected, if <param name="target"/> is Serology.
        /// E.g., A non-expressing allele will return the original typing, if <param name="target"/> is PGroup or Serology.
        /// E.g., An expressing allele with no known equivalent serology will return the original allele, if <param name="target"/> is Serology.</returns>
        public async Task<TransformationResult> ConvertRandomLocusHla(TransformationRequest request,
            string hlaNomenclatureVersion,
            TargetHlaCategory target)
        {
            if (request.Typings.IsNullOrEmpty())
            {
                return new TransformationResult();
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);
            var locus = request.Typings.First().Locus;

            return await TransformRandomlySelectedTypings(
                request,
                async hla => (await hlaMetadataDictionary.ConvertHla(locus, hla, target)).FirstOrDefault() ?? hla);
        }
    }
}