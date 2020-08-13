using System;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services.HlaMaskers
{
    internal interface IMacBuilder
    {
        Task<TransformationResult> ConvertRandomLocusHlaToMacs(TransformationRequest request);
    }

    internal class MacBuilder : HlaTransformerBase, IMacBuilder
    {
        public async Task<TransformationResult> ConvertRandomLocusHlaToMacs(TransformationRequest request)
        {
            // TODO: ATLAS-478
            throw new NotImplementedException();
        }
    }
}