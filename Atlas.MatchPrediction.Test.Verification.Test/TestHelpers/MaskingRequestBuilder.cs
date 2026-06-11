using System.Collections.Generic;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Test.Verification.Models;
using AutoFixture.Dsl;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;

internal static class MaskingRequestBuilder
{
    public static IPostprocessComposer<MaskingRequest> New => FixtureBuilder.For<MaskingRequest>();

    public static IPostprocessComposer<MaskingRequest> WithCategory(this IPostprocessComposer<MaskingRequest> builder, MaskingCategory category)
    {
        return builder.With(x => x.MaskingCategory, category);
    }

    public static IPostprocessComposer<MaskingRequest> WithCategories(this IPostprocessComposer<MaskingRequest> builder, IEnumerable<MaskingCategory> categories)
    {
        return builder.WithSequence(x => x.MaskingCategory, categories);
    }

    public static IPostprocessComposer<MaskingRequest> WithProportion(this IPostprocessComposer<MaskingRequest> builder, int proportion)
    {
        return builder.With(x => x.ProportionToMask, proportion);
    }
}