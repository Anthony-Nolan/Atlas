using Atlas.Common.Helpers;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal interface ITwoFieldBuilder
    {
        Task<TransformationResult> ConvertRandomLocusHlaToTwoField(TransformationRequest request);
    }

    internal class TwoFieldBuilder : HlaTransformerBase, ITwoFieldBuilder
    {
        public async Task<TransformationResult> ConvertRandomLocusHlaToTwoField(TransformationRequest request)
        {
            // Purposely not using the 2-field converter within the HMD as that offers more functionality than is required.
            // It fully expands a G group and returns 2-field versions of all its alleles. In many cases,
            // the selected "masked" typing would be of a higher resolution than the original genotype!

            return await TransformRandomlySelectedTypings(
                request,
                hlaName => Task.FromResult(AlleleSplitter.FirstTwoFieldsAsString(hlaName)));
        }
    }
}