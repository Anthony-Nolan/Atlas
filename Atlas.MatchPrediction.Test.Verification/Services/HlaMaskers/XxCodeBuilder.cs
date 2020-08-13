using System.Threading.Tasks;
using Atlas.Common.Helpers;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal interface IXxCodeBuilder
    {
        Task<TransformationResult> ConvertRandomLocusHlaToXxCodes(TransformationRequest request);
    }

    internal class XxCodeBuilder : HlaTransformerBase, IXxCodeBuilder
    {
        public async Task<TransformationResult> ConvertRandomLocusHlaToXxCodes(TransformationRequest request)
        {
            const string xxCodeSuffix = ":XX";
            return await TransformRandomlySelectedTypings(
                request, 
                hlaName => Task.FromResult($"{AlleleSplitter.FirstField(hlaName)}{xxCodeSuffix}"));
        }
    }
}