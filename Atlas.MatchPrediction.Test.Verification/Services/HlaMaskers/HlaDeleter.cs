using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal interface IHlaDeleter
    {
        Task<TransformationResult> DeleteRandomLocusHla(TransformationRequest request);
    }

    internal class HlaDeleter : HlaTransformerBase, IHlaDeleter
    {
        public async Task<TransformationResult> DeleteRandomLocusHla(TransformationRequest request)
        {
            return await TransformRandomlySelectedTypings(request, _ => Task.FromResult((string)null));
        }
    }
}