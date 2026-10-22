using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using NSubstitute;

namespace Atlas.MatchPrediction.Test.TestHelpers;

internal static class HaplotypeFrequencyLookupStub
{
    /// <summary>
    /// Stubs <see cref="IHaplotypeFrequencyLookupService.GetFrequenciesForHla"/> so that every haplotype asked for is
    /// unrepresented (0): one answer per haplotype, as the real service gives. Without it, NSubstitute returns an empty
    /// array, and the expander would read past its end.
    /// </summary>
    public static void ReturnsUnrepresentedForEveryHaplotype(this IHaplotypeFrequencyLookupService service)
    {
        service.GetFrequenciesForHla(default, default, default).ReturnsForAnyArgs(call =>
            Task.FromResult(new decimal[call.ArgAt<IReadOnlyList<LociInfo<string>>>(1).Count]));
    }
}
