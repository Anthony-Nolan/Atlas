using Atlas.Common.GeneticData.PhenotypeInfo;
using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    internal class MaskingRequests : LociInfo<IEnumerable<MaskingRequest>>
    {
        public MaskingRequests() : base(new List<MaskingRequest>())
        {
        }

        public MaskingRequests(LociInfo<IEnumerable<MaskingRequest>> source) : base(
            source.A,
            source.B,
            source.C,
            source.Dpb1,
            source.Dqb1,
            source.Drb1
        )
        {
        }
    }

    internal static class MaskingRequestsExtensions
    {
        public static MaskingRequests ToMaskingRequests(this MaskingRequestsTransfer transfer)
        {
            return new MaskingRequests(transfer.ToLociInfo()
                                       ?? new LociInfo<IEnumerable<MaskingRequest>>(new List<MaskingRequest>()));
        }
    }
}
