using System.Threading.Tasks;
using Atlas.Common.Helpers;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal interface IXxCodeBuilder
    {
        Task<TransformationResult> ConvertRandomLocusHlaToXxCodes(TransformationRequest request);
        string ConvertHlaToXxCode(string hla);
    }

    internal class XxCodeBuilder : HlaTransformerBase, IXxCodeBuilder
    {
        public async Task<TransformationResult> ConvertRandomLocusHlaToXxCodes(TransformationRequest request)
        {
            return await TransformRandomlySelectedTypings(
                request, 
                hlaName => Task.FromResult(ConvertHlaToXxCode(hlaName)));
        }

        public string ConvertHlaToXxCode(string hlaName)
        {
            const string xxCodeSuffix = ":XX";
            return $"{AlleleSplitter.FirstField(hlaName)}{xxCodeSuffix}";
        }
    }
}