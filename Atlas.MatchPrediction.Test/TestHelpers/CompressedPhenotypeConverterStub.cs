using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using NSubstitute;

namespace Atlas.MatchPrediction.Test.TestHelpers;

internal static class CompressedPhenotypeConverterStub
{
    /// <summary>
    /// Stubs <see cref="ICompressedPhenotypeConverter.ConvertPhenotype"/> from one
    /// <see cref="DataByResolution{T}"/> of groups, answering each category from the matching field.
    ///
    /// <para>
    /// The converter takes one call per typing category, so that the expander can convert only what it will read.
    /// Tests still say what the subject's groups are as a single object - the shape a reader can take in - and this
    /// splits it. It also leaves NSubstitute's own record of which categories were asked for intact, which is what
    /// <c>TypingCategoryConversionTests</c> asserts on.
    /// </para>
    /// </summary>
    internal static void StubGroups(
        this ICompressedPhenotypeConverter converter,
        DataByResolution<PhenotypeInfo<ISet<string>>> groups)
    {
        converter
            .ConvertPhenotype(Arg.Any<CompressedPhenotypeExpanderInput>(), Arg.Any<HaplotypeTypingCategory>())
            .Returns(call => groups.GetByCategory(call.Arg<HaplotypeTypingCategory>()));
    }
}
