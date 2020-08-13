using System.Collections.Generic;
using Atlas.MatchPrediction.Test.Verification.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers
{
    [Builder]
    internal static class MaskingRequestBuilder
    {
        public static Builder<MaskingRequest> New => Builder<MaskingRequest>.New;

        public static Builder<MaskingRequest> WithCategory(this Builder<MaskingRequest> builder, MaskingCategory category)
        {
            return builder.With(x => x.MaskingCategory, category);
        }

        public static Builder<MaskingRequest> WithCategories(this Builder<MaskingRequest> builder, IEnumerable<MaskingCategory> categories)
        {
            return builder.With(x => x.MaskingCategory, categories);
        }

        public static Builder<MaskingRequest> WithProportion(this Builder<MaskingRequest> builder, int proportion)
        {
            return builder.With(x => x.ProportionToMask, proportion);
        }
    }
}
