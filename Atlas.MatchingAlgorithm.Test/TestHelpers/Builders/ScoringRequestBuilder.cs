using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using LochNessBuilder;
using System.Collections.Generic;
using System.Linq;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    [Builder]
    public static class ScoringRequestBuilder
    {
        public static Builder<ScoringRequest<IEnumerable<MatchResult>>> New =>
            Builder<ScoringRequest<IEnumerable<MatchResult>>>.New
                .With(x => x.PatientHla, new PhenotypeInfo<string>())
                .With(x => x.DonorData, new[] { new MatchResultBuilder().Build() })
                .With(x => x.LociToScore, EnumerateValues<Locus>().ToList());
    }
}
